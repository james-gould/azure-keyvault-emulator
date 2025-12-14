using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys;

// https://learn.microsoft.com/en-us/rest/api/keyvault/keys/get-keys/get-keys?view=rest-keyvault-keys-7.4&tabs=HTTP#keyitem
public sealed class KeyItemBundle : TaggedModel
{
    [JsonPropertyName("attributes")]
    public KeyAttributes KeyAttributes { get; set; } = new();

    [JsonPropertyName("kid")]
    public required string KeyId { get; set; }

    [JsonPropertyName("managed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Managed { get; set; } = null;
}
