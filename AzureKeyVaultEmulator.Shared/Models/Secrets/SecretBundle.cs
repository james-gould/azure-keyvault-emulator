using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets
{
    public sealed class SecretBundle : ResponseBase
    {
        [JsonPropertyName("id")]
        public Uri? Id { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public SecretAttributesModel Attributes { get; set; } = new();
    }
}
