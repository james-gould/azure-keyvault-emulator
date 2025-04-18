using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets;

public sealed class SecretProperties
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;
}
