using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Shared.Models
{
    public class AttributeBase
    {
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("exp")]
        public long Expiration { get; set; }

        [JsonPropertyName("nbf")]
        public long NotBefore { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("updated")]
        public long Updated { get; set; }

        [JsonPropertyName("recoveryLevel")]
        public string RecoveryLevel = DeletionRecoveryLevel.Purgable.ToString();

        public void Update() => Updated = DateTimeOffset.Now.ToUnixTimeSeconds();
    }
}
