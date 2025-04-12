using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys;

public class SignKeyModel
{
    [JsonPropertyName("alg")]
    public required string SigningAlgorithm { get; set; }

    [JsonPropertyName("value")]
    public required string Value { get; set; }
}
