using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets.Requests
{
    public class SetSecretRequest : ICreateItem
    {
        [JsonPropertyName("value")]
        public required string Value { get; set; }

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public SecretAttributes SecretAttributes { get; set; } = new();

        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; } = [];
    }
}
