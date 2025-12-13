using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Hollow_IM_Server.Classes.Models
{
    internal class MessageModel
    {
        [JsonPropertyName("user")]
        public required UserModel User { get; set; }

        [JsonPropertyName("sent_at")]
        public required DateTime SentAt { get; set; }

        [JsonPropertyName("content")]
        public required string Content { get; set; }

    }
}
