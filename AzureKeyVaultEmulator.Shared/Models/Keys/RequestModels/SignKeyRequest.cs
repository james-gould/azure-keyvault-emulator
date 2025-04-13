using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys.RequestModels;

public class SignKeyRequest
{
    [JsonPropertyName("alg")]
    public required string SigningAlgorithm { get; set; }

    [JsonPropertyName("value")]
    public required string Value { get; set; }
}
