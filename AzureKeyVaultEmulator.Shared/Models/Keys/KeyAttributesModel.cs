using AzureKeyVaultEmulator.Shared.Models;
using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys
{
    public class KeyAttributesModel : AttributeBase
    {
        [JsonPropertyName("recoverableDays")]
        public int RecoverableDays { get; set; }
    }
}
