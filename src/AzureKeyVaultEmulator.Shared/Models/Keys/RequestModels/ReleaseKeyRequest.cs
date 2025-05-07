using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys.RequestModels;

public sealed class ReleaseKeyRequest
{
    [JsonPropertyName("target")]
    public required string Target { get; set; }

    [JsonPropertyName("enc")]
    public string? EncryptionAlgorithm { get; set; }

    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; }

}
