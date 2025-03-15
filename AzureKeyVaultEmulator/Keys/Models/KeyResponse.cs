using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Keys.Models
{
    public class KeyResponse
    {
        [JsonPropertyName("key")]
        public JsonWebKeyModel Key { get; set; } = new();

        [JsonPropertyName("attributes")]
        public KeyAttributesModel Attributes { get; set; } = new();

        [JsonPropertyName("tags")]
        public object? Tags { get; set; }
    }
}
