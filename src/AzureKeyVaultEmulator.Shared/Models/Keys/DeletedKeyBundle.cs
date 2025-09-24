using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys;

public sealed class DeletedKeyBundle : DeletedBundle<KeyAttributes>
{
    [JsonPropertyName("key")]
    public required Microsoft.IdentityModel.Tokens.JsonWebKey Key { get; set; }
}
