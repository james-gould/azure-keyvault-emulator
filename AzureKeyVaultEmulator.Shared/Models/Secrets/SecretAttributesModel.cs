using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets
{
    public class SecretAttributesModel : AttributeBase
    {
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = string.Empty;
    }
}
