using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models
{
    public class ValueModel<T>
    {
        [JsonPropertyName("value")]
        public required T Value { get; set; }
    }
}
