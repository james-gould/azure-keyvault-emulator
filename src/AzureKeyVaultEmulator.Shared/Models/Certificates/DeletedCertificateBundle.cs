using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

public sealed class DeletedCertificateBundle : DeletedBundle<CertificateAttributesModel>
{
    [JsonPropertyName("id")]
    public string CertificateIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("x5t")]
    public string CertificateThumbprint { get; set; } = string.Empty;
    
    [JsonPropertyName("cer")]
    public string CertBase64 { get; set; } = string.Empty;

    [JsonPropertyName("sid")]
    public string SecretId { get; set; } = string.Empty;

    [JsonPropertyName("policy")]
    public CertificatePolicy Policy { get; set; } = new();

    /// <summary>
    /// Just to be lazy, allows us to revert back the CertificateBundle via /recover api call
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public CertificateBundle? FullCertificate { get; set; }
}
