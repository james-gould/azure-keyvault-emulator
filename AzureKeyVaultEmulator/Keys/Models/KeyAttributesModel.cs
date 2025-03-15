using AzureKeyVaultEmulator.Shared.Models;
using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Keys.Models
{
    public class KeyAttributesModel : AttributeBase
    {
        [JsonPropertyName("created")]
        public int Created { get; set; }

        [JsonPropertyName("recoverableDays")]
        public int RecoverableDays { get; set; }

        [JsonPropertyName("recoveryLevel")]
        public string RecoveryLevel { get; set; } = string.Empty;

        [JsonPropertyName("updated")]
        public int Updated { get; set; }
    }
}
