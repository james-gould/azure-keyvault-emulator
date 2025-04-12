using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace AzureKeyVaultEmulator.Shared.Models.Keys;

public sealed class DeletedKeyBundle : DeletedBundle<KeyAttributesModel>
{
    [JsonPropertyName("recoveryId")]
    public required string RecoveryId { get; set; }

    [JsonPropertyName("key")]
    public required JsonWebKey Key { get; set; }
}
