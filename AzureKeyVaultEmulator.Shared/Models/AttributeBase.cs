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
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("exp")]
        public int Expiration { get; set; }

        [JsonPropertyName("nbf")]
        public int NotBefore { get; set; }
    }
}
