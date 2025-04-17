using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets;

public sealed class SecretProperties
{
    [JsonPropertyName("contentType")]
    public required string ContentType { get; set; }
}
