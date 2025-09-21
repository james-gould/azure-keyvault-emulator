using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

public sealed class CertificateAttributes : AttributeBase
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}
