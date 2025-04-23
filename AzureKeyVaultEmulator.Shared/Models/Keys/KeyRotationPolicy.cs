using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Constants;

namespace AzureKeyVaultEmulator.Shared.Models.Keys;

public sealed class KeyRotationPolicy
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public KeyRotationAttributes Attributes { get; set; } = new();

    public IEnumerable<LifetimeActions> LifetimeActions { get; set; } = [];

    public void SetIdFromKeyName(string keyName)
        => Id = $"{AuthConstants.EmulatorUri}/keys/{keyName}/rotationpolicy";
}

public class KeyRotationAttributes
{
    [JsonPropertyName("expiryTime")]
    public string ExpiryTime { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public long CreatedTsUnix { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds();

    [JsonPropertyName("updated")]
    public long UpdatedTsUnix { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds();

    public void Update() => UpdatedTsUnix = DateTimeOffset.Now.ToUnixTimeSeconds();
}
