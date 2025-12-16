using System.Text.Json.Serialization;

namespace Hollow_IM_Server.Classes.Models
{
    internal class ClientChatState
    {
        [JsonPropertyName("messages_state")]
        public required int MessagesState { get; set; }

        [JsonPropertyName("users_state")]
        public required int UsersState { get; set; }
    }
}
