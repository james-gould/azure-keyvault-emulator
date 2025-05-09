using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets
{
    public class DeletedSecretBundle : DeletedBundle<SecretAttributesModel>, INamedItem
    {

        [JsonPropertyName("id")]
        public string SecretId { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}
