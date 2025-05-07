using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;

public sealed class UpdateCertificateRequest : TaggedModel
{
    [JsonPropertyName("attributes")]
    public CertificateAttributesModel? Attributes { get; set; }

    [JsonPropertyName("policy")]
    public CertificatePolicy? Policy { get; set; }
}
