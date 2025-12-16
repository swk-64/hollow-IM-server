using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Hollow_IM_Server.Classes.Models
{
    internal class UserModel
    {
        [JsonPropertyName("username")]
        public required string Username { get; set; }
    }
}
