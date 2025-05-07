using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys;

public sealed class KeyReleasePolicy
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    [JsonPropertyName("immutable")]
    public bool Immutable { get;set; }
}
