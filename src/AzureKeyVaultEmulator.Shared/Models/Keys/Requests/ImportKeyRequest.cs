using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace AzureKeyVaultEmulator.Shared.Models.Keys.RequestModels;

public sealed class ImportKeyRequest
{
    [JsonPropertyName("key")]
    public required Microsoft.IdentityModel.Tokens.JsonWebKey Key { get; set; }

    [JsonPropertyName("hsm")]
    public bool? HSM { get; set; }

    [JsonPropertyName("attributes")]
    public KeyAttributes KeyAttributes { get; set; } = new();

    [JsonPropertyName("release_policy")]
    public object? ReleasePolicy { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string> Tags { get; set; } = [];
}
