using Hollow_IM_Server.Classes.Models;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Hollow_IM_Server.Classes
{
    internal class ResponseManager
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
        public static void JoinChat(SslStream stream, bool status, ChatModel? chat = null) // if status is false, the model won't be used anyway
        {
            JsonElement payload;

            if (status)
            {
                string chatStr = JsonSerializer.Serialize(chat);
                using var chatJson = JsonDocument.Parse(chatStr);
                payload = chatJson.RootElement.Clone();
            }
            else
            {
                // Empty JSON object
                using var emptyJson = JsonDocument.Parse("{}");
                payload = emptyJson.RootElement.Clone();
            }
            var response = new Response { Action = "JOIN_CHAT", Status = status, Payload = payload };

            var packet = BuildResponsePacket(response);
            stream.Write(packet, 0, packet.Length);

            return;
        }
        public static void SendMessage(SslStream stream, bool status, MessageModel? message = null) // if status is false, the model won't be used anyway
        {
            JsonElement payload;

            if (status)
            {
                string messageStr = JsonSerializer.Serialize(message);
                using var messageJson = JsonDocument.Parse(messageStr);
                payload = messageJson.RootElement.Clone();
            }
            else
            {
                // Empty JSON object
                using var emptyJson = JsonDocument.Parse("{}");
                payload = emptyJson.RootElement.Clone();
            }

            var response = new Response { Action = "SEND_MESSAGE", Status = status, Payload = payload };

            var packet = BuildResponsePacket(response);
            stream.Write(packet, 0, packet.Length);

            return;
        }
        public static void SyncChat(SslStream stream, bool status, SyncChatModel? diff = null) // if status is false, the model won't be used anyway
        {
            JsonElement payload;

            if (status)
            {
                string diffStr = JsonSerializer.Serialize(diff);
                using var diffJson = JsonDocument.Parse(diffStr);
                payload = diffJson.RootElement.Clone();
            }
            else
            {
                // Empty JSON object
                using var emptyJson = JsonDocument.Parse("{}");
                payload = emptyJson.RootElement.Clone();
            }
            var request = new Response { Action = "SYNC_CHAT", Status = status, Payload = payload };

            var packet = BuildResponsePacket(request);
            stream.Write(packet, 0, packet.Length);

            return;
        }
    }
}
