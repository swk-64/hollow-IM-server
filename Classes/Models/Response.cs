using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hollow_IM_Server.Classes.Models
{
    internal class Response
    {
        // the status is a bool value. true = success, false = fail
        [JsonPropertyName("action")]
        public required string Action { get; set; }

        [JsonPropertyName("status")]
        public required bool Status { get; set; }

        [JsonPropertyName("payload")]
        public JsonElement Payload { get; set; }
    }
}
