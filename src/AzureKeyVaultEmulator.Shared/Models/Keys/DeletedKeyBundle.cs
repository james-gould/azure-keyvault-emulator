using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace AzureKeyVaultEmulator.Shared.Models.Keys;

public sealed class DeletedKeyBundle : DeletedBundle<KeyAttributesModel>
{
    [JsonPropertyName("key")]
    public required JsonWebKey Key { get; set; }
}
