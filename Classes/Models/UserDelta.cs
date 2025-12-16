using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Hollow_IM_Server.Classes.Models
{
    internal class UserDelta
    {
        [JsonPropertyName("change_type")]
        public required bool ChangeType { get; set; }

        [JsonPropertyName("user_to_change")]
        public required UserModel UserToChange { get; set; }
    }
}
