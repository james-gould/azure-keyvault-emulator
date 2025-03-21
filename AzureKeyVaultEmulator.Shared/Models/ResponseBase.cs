using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Shared.Models
{
    public class ResponseBase
    {
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; } = [];
    }
}
