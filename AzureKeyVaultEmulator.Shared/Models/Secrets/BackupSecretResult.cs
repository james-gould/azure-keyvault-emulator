using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets
{
    public class BackupSecretResult
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}
