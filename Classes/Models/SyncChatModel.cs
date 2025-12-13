using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Hollow_IM_Server.Classes.Models
{
    internal class ChatDelta
    {
        [JsonPropertyName("last_messages_state")]
        public required int LastMessagesState { get; set; }

        [JsonPropertyName("messages_delta")]
        public required List<MessageModel> MessagesDelta { get; set; }

        [JsonPropertyName("last_users_state")]
        public required int LastUsersState{ get; set; }

        [JsonPropertyName("users_delta")]
        public required List<UserDelta> UsersDelta{ get; set; }
    }
}
