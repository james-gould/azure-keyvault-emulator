using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

public sealed class CertificateAttributesModel : AttributeBase
{
    [JsonPropertyName("recoverableDays")]
    public int RecoverableDays { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}
