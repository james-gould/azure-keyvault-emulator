using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Shared.Models
{
    public sealed class KeyVaultError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("innererror")]
        public string InnerError => "No inner error provided with emulated key vault";

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
