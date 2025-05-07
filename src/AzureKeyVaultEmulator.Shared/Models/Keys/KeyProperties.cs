using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys;

public sealed class KeyProperties
{
    [JsonPropertyName("crv")]
    public string JsonWebKeyCurveName { get; set; } = string.Empty;

    [JsonPropertyName("exportable")]
    public static bool Exportable => true;

    [JsonPropertyName("key_size")]
    public int KeySize { get; set; } = 2048;

    [JsonPropertyName("kty")]
    public string JsonWebKeyType { get; set; } = string.Empty;

    [JsonPropertyName("reuse_key")]
    public bool ReuseKey { get; set; }
}
