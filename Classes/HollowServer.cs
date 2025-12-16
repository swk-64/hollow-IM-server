using Hollow_IM_Server.Classes.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Hollow_IM_Server.Classes
{
    internal class HollowServer
    {
        private readonly TcpListener server;
        private readonly ConcurrentDictionary<Guid, ConnectedClient> connectedClients;
        private readonly DBManager dbManager;
        private readonly X509Certificate2 serverCert;

        public HollowServer(int port, string address, string connString, string pkcs12Password)
        {
            var addr = IPAddress.Parse(address);
            server = new TcpListener(addr, port);
            dbManager = new DBManager(connString);

            connectedClients = new ConcurrentDictionary<Guid, ConnectedClient>();

            string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string certPath = Path.Combine(docsPath, "hollow-IM", "certificates", "server.pfx");

            serverCert = X509CertificateLoader.LoadPkcs12FromFile(certPath, pkcs12Password);

        }

        public async Task StartListeningAsync()
        {
            server.Start();
            while (true)
            {
                Console.WriteLine("Waiting for a connection... ");

                TcpClient client = await server.AcceptTcpClientAsync();

                ConnectedClient connected_client = new ConnectedClient { TcpClient = client, User = null };

                Console.WriteLine("A user connected!");

                connectedClients.TryAdd(connected_client.Id, connected_client);

                _ = HandleClientAsync(connected_client);

            }
        }

        private async Task<Request?> readResponseAsync(SslStream secured_stream)
        {
            byte[] prefixBuffer = new byte[4];
            using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                await secured_stream!.ReadExactlyAsync(prefixBuffer, 0, 4, cts1.Token);

                Int32 length = BitConverter.ToInt32(prefixBuffer, 0);
                byte[] requestBuffer = new byte[length];

                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await secured_stream.ReadExactlyAsync(requestBuffer, 0, length, cts2.Token);

                string requestJson = Encoding.UTF8.GetString(requestBuffer);

                return JsonSerializer.Deserialize<Request>(requestJson)!;

            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Read timed out after 5 seconds.");
                return null;
            }
            catch (EndOfStreamException)
            {
                Console.WriteLine("Stream ended before enough bytes were read.");
                return null;
            }

        }

        private async Task HandleClientAsync(ConnectedClient current_client)
        {
            using var stream = current_client.TcpClient.GetStream();

            using var secured_stream = new SslStream(stream, false);

            await secured_stream.AuthenticateAsServerAsync(
                serverCert, 
                clientCertificateRequired: false, 
                enabledSslProtocols: SslProtocols.Tls13, 
                checkCertificateRevocation: false
                );



            try
            {
                while (true)
                {
                    Request? request = await readResponseAsync(secured_stream);
                    if (request == null)
                        break;

                    switch (request.Action)
                    {
                        case "JOIN_CHAT":
                            {
                                var user = request.Payload.Deserialize<UserModel>();

                                if (string.IsNullOrWhiteSpace(user?.Username))
                                {
                                    ResponseManager.JoinChat(secured_stream, false);
                                    break;
                                }

                                var chat = await dbManager.JoinChat(user);

                                current_client.User = user;

                                chat.Users.AddRange(connectedClients.Values.ToList().Select(c => c.User!));

                                ResponseManager.JoinChat(secured_stream, true, chat);

                                break;
                            }
                        case "SEND_MESSAGE":
                            {
                                //check if user set its name
                                if (current_client.User == null)
                                {
                                    ResponseManager.SendMessage(secured_stream, false);
                                    break;
                                }

                                var message = request.Payload.Deserialize<MessageModel>();

                                //check if a message content isn't empty
                                if (string.IsNullOrWhiteSpace(message?.Content))
                                {
                                    ResponseManager.SendMessage(secured_stream, false);
                                    break;
                                }

                                //Validate message sender
                                if (current_client.User != message?.User)
                                {
                                    ResponseManager.SendMessage(secured_stream, false);
                                    break;
                                }

                                // Time validation isn't implemented

                                await dbManager.SendMessage(message);

                                ResponseManager.SendMessage(secured_stream, true, message);

                                break;
                            }
                        case "SYNC_CHAT":
                            {
                                //check if user set its name
                                if (current_client.User == null)
                                {
                                    ResponseManager.SyncChat(secured_stream, false);
                                    break;
                                }

                                var state = request.Payload.Deserialize<ClientChatState>();

                                if (state?.MessagesState == null)
                                {
                                    ResponseManager.SyncChat(secured_stream, false);
                                    break;
                                }

                                var diff = await dbManager.SyncChat(state);

                                diff.Users.AddRange(connectedClients.Values.ToList().Select(c => c.User!));

                                ResponseManager.SyncChat(secured_stream, true, diff);

                                break;
                            }
                    }
                    await secured_stream.FlushAsync(); // ensure data is sent
                }
            }
            catch (IOException)
            {
                Console.WriteLine("Client disconnected unexpectedly.");
            }
            finally
            {
                current_client.TcpClient.Dispose();

                connectedClients.TryRemove(current_client.Id, out _);
            }
        }
    }
}
