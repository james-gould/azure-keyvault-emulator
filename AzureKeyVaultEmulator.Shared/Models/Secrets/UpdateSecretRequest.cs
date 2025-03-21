using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets
{
    public class UpdateSecretRequest
    {
        [JsonPropertyName("attributes")]
        public SecretAttributesModel? Attributes { get; set; }

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; } = [];
    }
}
