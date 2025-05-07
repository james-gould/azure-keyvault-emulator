using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets
{
    public class DeletedSecretBundle : DeletedBundle<SecretAttributesModel>
    {
        [JsonPropertyName("id")]
        public string SecretId { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}
