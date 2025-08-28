using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys.RequestModels;

public sealed class VerifyHashRequest
{
    [JsonPropertyName("alg")]
    public required string Algorith { get; set; }

    [JsonPropertyName("digest")]
    public required string Digest { get; set; }

    [JsonPropertyName("value")]
    public required string Value { get; set; }
}
