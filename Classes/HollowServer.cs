using Hollow_IM_Server.Classes.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;

namespace Hollow_IM_Server.Classes
{
    internal class HollowServer
    {

        private readonly TcpListener server;
        private readonly ConcurrentDictionary<Guid, ConnectedClient> connectedClients;
        private readonly DBManager dbManager;

        // local host is "127.0.0.1"
        public HollowServer(Int32 port, string address, string connString)
        {
            var addr = IPAddress.Parse(address);
            server = new TcpListener(addr, port);
            dbManager = new DBManager(connString);

            connectedClients = new ConcurrentDictionary<Guid, ConnectedClient>();
        }

        public async Task StartListeningAsync()
        {
            server.Start();
            while (true)
            {
                Console.WriteLine("Waiting for a connection... ");
                // Accept incoming client connection
                TcpClient client = await server.AcceptTcpClientAsync();

                ConnectedClient connected_client = new ConnectedClient { TcpClient = client, User = null };

                Console.WriteLine("Connected!");

                connectedClients.TryAdd(connected_client.Id, connected_client);

                _ = HandleClientAsync(connected_client);

            }
        }

        private async Task<Request?> readResponseAsync(NetworkStream clientStream)
        {
            byte[] prefixBuffer = new byte[4];
            using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                await clientStream!.ReadExactlyAsync(prefixBuffer, 0, 4, cts1.Token);

                Int32 length = BitConverter.ToInt32(prefixBuffer, 0);
                byte[] requestBuffer = new byte[length];

                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await clientStream.ReadExactlyAsync(requestBuffer, 0, length, cts2.Token);

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
            using NetworkStream stream = current_client.TcpClient.GetStream();
            //using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            try
            {
                while (true)
                {
                    Request? request = await readResponseAsync(stream);
                    if (request == null)
                        break;

                    switch (request.Action)
                    {
                        case "JOIN_CHAT":
                            {
                                var user = request.Payload.Deserialize<UserModel>();

                                if (string.IsNullOrWhiteSpace(user?.Username))
                                {
                                    HollowProtocol.JoinChat(stream, false);
                                    break;
                                }

                                var chat = await dbManager.JoinChat(user);

                                current_client.User = user;

                                chat.Users.AddRange(connectedClients.Values.ToList().Select(c => c.User!));

                                HollowProtocol.JoinChat(stream, true, chat);

                                break;
                            }
                        case "SEND_MESSAGE":
                            {
                                //check if user set its name
                                if (current_client.User == null)
                                {
                                    HollowProtocol.SendMessage(stream, false);
                                    break;
                                }

                                var message = request.Payload.Deserialize<MessageModel>();

                                //check if a message content isn't empty
                                if (string.IsNullOrWhiteSpace(message?.Content))
                                {
                                    HollowProtocol.SendMessage(stream, false);
                                    break;
                                }

                                //Validate message sender
                                if (current_client.User != message?.User)
                                {
                                    HollowProtocol.SendMessage(stream, false);
                                    break;
                                }

                                // Time validation isn't implemented

                                await dbManager.SendMessage(message);

                                HollowProtocol.SendMessage(stream, true, message);

                                break;
                            }
                        case "SYNC_CHAT":
                            {
                                //check if user set its name
                                if (current_client.User == null)
                                {
                                    HollowProtocol.SyncChat(stream, false);
                                    break;
                                }

                                var state = request.Payload.Deserialize<ClientChatState>();

                                if (state?.MessagesState == null)
                                {
                                    HollowProtocol.SyncChat(stream, false);
                                    break;
                                }

                                var diff = await dbManager.SyncChat(state);

                                diff.Users.AddRange(connectedClients.Values.ToList().Select(c => c.User!));

                                HollowProtocol.SyncChat(stream, true, diff);

                                break;
                            }
                    }
                    await stream.FlushAsync(); // ensure data is sent
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
