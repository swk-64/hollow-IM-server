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
        private readonly ConcurrentBag<TcpClient> connectedClients;

        // local host is "127.0.0.1"
        public HollowServer(Int32 port, string address)
        {
            var addr = IPAddress.Parse(address);
            server = new TcpListener(addr, port);

            connectedClients = new ConcurrentBag<TcpClient>();
        }

        public async Task StartListeningAsync()
        {
            server.Start();
            while (true)
            {
                Console.WriteLine("Waiting for a connection... ");
                // Accept incoming client connection
                TcpClient client = await server.AcceptTcpClientAsync();
                Console.WriteLine("Connected!");

                connectedClients.Add(client);

                _ = HandleClientAsync(client);

            }
        }

        private async Task<Response?> readResponseAsync(NetworkStream clientStream)
        {
            byte[] prefixBuffer = new byte[4];
            using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                await clientStream!.ReadExactlyAsync(prefixBuffer, 0, 4, cts1.Token);

                Int32 length = BitConverter.ToInt32(prefixBuffer, 0);
                byte[] responseBuffer = new byte[length];

                using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await clientStream.ReadExactlyAsync(responseBuffer, 0, length, cts2.Token);

                string requestJson = Encoding.UTF8.GetString(responseBuffer);

                return JsonSerializer.Deserialize<Response>(requestJson)!;

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

        private async Task HandleClientAsync(TcpClient client)
        {
            using NetworkStream stream = client.GetStream();
            //using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            try
            {
                await BroadcastAsync($"User joined: {client.Client.RemoteEndPoint}");

                while (true)
                {
                    Response? request = await readResponseAsync(stream);
                    if (request == null)
                        break;

                    // Broadcast to others
                    await BroadcastAsync($"Message from {client.Client.RemoteEndPoint}: {payload}");

                    // Example: send back an acknowledgment
                    string response = $"Echo: {payload}";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    writer.Write(responseBytes.Length);
                    writer.Write(responseBytes);

                    await stream.FlushAsync(); // ensure data is sent
                }
            }
            catch (IOException)
            {
                Console.WriteLine("Client disconnected unexpectedly.");
            }
            finally
            {
                await BroadcastAsync($"User left: {client.Client.RemoteEndPoint}");
                client.Close();
            }
        }
        static async Task BroadcastAsync(string message)
        {
            foreach (var c in connectedClients)
            {
                try
                {
                    var writer = new BinaryWriter(c.GetStream(), Encoding.UTF8, leaveOpen: true);
                    await writer.WriteLineAsync(message); // async write
                }
                catch
                {
                    // Ignore failed clients
                }
            }
        }
    }
}
