using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models
{
    public class ValueModel
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}
