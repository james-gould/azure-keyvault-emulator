using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

public sealed class CertificateBundle : CertificateProperties
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long PrimaryId { get; set; }

    [JsonPropertyName("policy")]
    public CertificatePolicy? CertificatePolicy { get; set; }

    [JsonPropertyName("cer")]
    public string CertificateContents { get; set; } = string.Empty;

    [JsonPropertyName("kid")]
    public string KeyId { get; set; } = string.Empty;

    [JsonPropertyName("sid")]
    public string SecretId { get; set; } = string.Empty;

    public byte[] CertificateBlob { get; set; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    private X509Certificate2? _certificate;

    /// <summary>
    /// <para>Here to facilitate testing, we need the RSA private key available.</para>
    /// <para>Cer is only the public key information, only other option is we hardcode an export or have files on disk.</para>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public X509Certificate2? FullCertificate
    {
        get
        {
            if(_certificate != null)
                return _certificate;

            return CertificateBlob is null || CertificateBlob.Length == 0
                ? null
                : CertificateBlobSerializer.Deserialize(CertificateBlob, "emulator");
        }
        set
        {
            CertificateBlob =
                value is null ? [] : CertificateBlobSerializer.Serialize(value, "emulator");
                
        }
    }
}

public static class CertificateBundleCloning
{
    public static CertificateBundle CopyWithNewCertificate(this CertificateBundle bundle, X509Certificate2 newCertificate)
    {
        return new()
        {
            CertificateIdentifier = bundle.CertificateIdentifier,
            CertificatePolicy = bundle.CertificatePolicy,
            CertificateContents = Convert.ToBase64String(newCertificate.RawData),
            KeyId = bundle.KeyId,
            SecretId = bundle.SecretId,
            Attributes = bundle.Attributes,
            CertificateName = bundle.CertificateName,
            VaultUri = bundle.VaultUri,
            X509Thumbprint = newCertificate.Thumbprint,
            Tags = bundle.Tags
        };
    }
}
