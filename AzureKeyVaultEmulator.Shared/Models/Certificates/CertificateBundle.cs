using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates
{
    public sealed class CertificateBundle : CertificateProperties
    {
        [JsonPropertyName("policy")]
        public CertificatePolicy? CertificatePolicy { get; set; }

        [JsonPropertyName("cer")]
        public string CertificateContents { get; set; } = string.Empty;

        [JsonPropertyName("kid")]
        public string KeyIdentifier { get; set; } = string.Empty;

        [JsonPropertyName("sid")]
        public string SecretId { get; set; } = string.Empty;
    }
}
