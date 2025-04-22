using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models
{
    public class TaggedModel
    {
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; } = [];
    }
}
