using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys;

public sealed class KeyProperties
{
    [JsonPropertyName("crv")]
    public required string JsonWebKeyCurveName { get; set; }

    [JsonPropertyName("exportable")]
    public static bool Exportable => true;

    [JsonPropertyName("key_size")]
    public required int KeySize { get; set; }

    [JsonPropertyName("kty")]
    public required string JsonWebKeyType { get; set; }

    [JsonPropertyName("reuse_key")]
    public bool ReuseKey { get; set; }
}
