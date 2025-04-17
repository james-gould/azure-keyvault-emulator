using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Constants;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates
{
    public sealed class CertificateBundle : ResponseBase
    {
        public CertificateBundle(X509ContentType contentType)
        {
            ContentType = contentType.ToApplicationContentType();
        }

        [JsonPropertyName("id")]
        public required string CertificateIdentifier { get; set; }

        [JsonPropertyName("attributes")]
        public required CertificateAttributesModel Attributes { get; set; }

        [JsonPropertyName("cer")]
        public required string CertificateContents { get; set; }

        [JsonPropertyName("contentType")]
        public string ContentType { get; private set; }

        [JsonPropertyName("kid")]
        public required string KeyIdentifier { get; set; }

        [JsonPropertyName("policy")]
        public CertificatePolicy? CertificatePolicy { get; set; }

        [JsonPropertyName("sid")]
        public string SecretId { get; set; } = string.Empty;

        [JsonPropertyName("x5t")]
        public required string Thumbprint { get; set; }
    }
}
