using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Hollow_IM_Server.Classes.Models;

namespace Hollow_IM_Server.Classes
{
    internal class HollowProtocol
    {
        private static Byte[] BuildResponsePacket(Response response)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

            string serialized = JsonSerializer.Serialize(response);
            Byte[] bytes = Encoding.UTF8.GetBytes(serialized);

            // Write Length Prefix (int32)
            writer.Write(bytes.Length);

            // Write Payload
            writer.Write(bytes);

            byte[] packet = ms.ToArray();
            return packet;
        }
        public static void JoinChat(NetworkStream stream, bool status, ChatModel chat)
        {
            if (status)
            {
                string chatStr = JsonSerializer.Serialize<ChatModel>(chat);
                var chatJson = JsonDocument.Parse(chatStr);

                var response = new Response { Action = "JOIN_CHAT", Status = true, Payload = chatJson.RootElement.Clone() };
                var packet = BuildResponsePacket(response);
                stream.Write(packet, 0, packet.Length);
            }
            else
            {
                var response = new Response { Action = "JOIN_CHAT", Status = false };
                var packet = BuildResponsePacket(response);
                stream.Write(packet, 0, packet.Length);
            }
            return;
        }
        public static void SendMessage(NetworkStream stream, bool status, MessageModel message)
        {
            string messageStr = JsonSerializer.Serialize<MessageModel>(message);
            var messageJson = JsonDocument.Parse(messageStr);

            var request = new Response { Action = "SEND_MESSAGE", Payload = messageJson.RootElement.Clone() };

            var packet = BuildRequestPacket(request);
            stream.Write(packet, 0, packet.Length);

            return;
        }
        public static void SyncChat(NetworkStream stream, int messageState, int userState)
        {
            string stateStr = JsonSerializer.Serialize<int>(messageState);
            var stateJson = JsonDocument.Parse(stateStr);

            var request = new Models.Response { Action = "SYNC_CHAT", Payload = stateJson.RootElement.Clone() };

            var packet = BuildRequestPacket(request);
            stream.Write(packet, 0, packet.Length);

            return;
        }
    }
}
