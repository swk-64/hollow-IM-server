using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Hollow_IM_Server.Classes.Models
{
    internal class ChatModel
    {
        [JsonPropertyName("me")]
        public required UserModel Me { get; set; }

        [JsonPropertyName("messages_state")]
        public required int MessagesState { get; set; }

        [JsonPropertyName("messages")]
        public required List<MessageModel> Messages { get; set; }

        [JsonPropertyName("users_state")]
        public required int UsersState { get; set; }

        [JsonPropertyName("users")]
        public required List<UserModel> Users { get; set; }
    }
}
