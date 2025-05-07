using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models
{
    public sealed class KeyVaultError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("innererror")]
        public string InnerError { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
