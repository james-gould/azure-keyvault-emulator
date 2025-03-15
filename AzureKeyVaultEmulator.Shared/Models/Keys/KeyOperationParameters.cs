using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys
{
    public class KeyOperationParameters
    {
        [JsonPropertyName("alg")]
        public string Algorithm { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Data { get; set; } = string.Empty;
    }
}
