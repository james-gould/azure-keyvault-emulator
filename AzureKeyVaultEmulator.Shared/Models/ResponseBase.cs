using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models
{
    public class ResponseBase
    {
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; } = [];
    }
}
