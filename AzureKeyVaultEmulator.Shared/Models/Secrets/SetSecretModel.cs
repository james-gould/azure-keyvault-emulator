using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets
{
    public class SetSecretModel : ICreateItem
    {
        [JsonPropertyName("value")]
        [Required]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public SecretAttributesModel SecretAttributes { get; set; } = new();

        [JsonPropertyName("tags")]
        public object? Tags { get; set; }
    }
}
