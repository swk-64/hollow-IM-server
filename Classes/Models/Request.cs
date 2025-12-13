using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Hollow_IM_Client.Classes.Models
{
    internal class Request
    {
        [JsonPropertyName("action")]
        public required string Action { get; set; }

        [JsonPropertyName("payload")]
        public JsonElement Payload { get; set; }
    }
}
