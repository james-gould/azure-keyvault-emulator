using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Keys.Models
{
    public class KeyOperationParameters
    {
        [JsonPropertyName("alg")]
        public string Algorithm { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Data { get; set; } = string.Empty;
    }
}
