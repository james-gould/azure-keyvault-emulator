using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys
{
    public class KeyOperationResult
    {
        [JsonPropertyName("kid")]
        public string KeyIdentifier { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Data { get; set; } = string.Empty;
    }
}
