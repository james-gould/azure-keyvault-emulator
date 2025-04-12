using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Shared
{
    public class ValueResponse
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}
