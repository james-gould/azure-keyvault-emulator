using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models
{
    /// <summary>
    /// For models that have a "value" property, which can be one of many types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueModel<T>
    {
        [JsonPropertyName("value")]
        public required T Value { get; set; }
    }
}
