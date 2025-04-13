using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys.RequestModels;

public sealed class UpdateKeyRequest
{
    [JsonPropertyName("attributes")]
    public KeyAttributesModel Attributes { get; set; } = new();

    [JsonPropertyName("tags")]
    public Dictionary<string, string> Tags { get; set; } = [];

    [JsonPropertyName("key_ops")]
    public IEnumerable<string> KeyOperations { get; set; } = [];

    [JsonPropertyName("release_policy")]
    public KeyReleasePolicy KeyReleasePolicy { get; set; } = new();
}
