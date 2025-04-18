using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;

public sealed class CreateCertificateRequest
{
    [JsonPropertyName("attributes")]
    public CertificateAttributesModel Attributes { get; set; } = new();

    [JsonPropertyName("policy")]
    public CertificatePolicy? CertificatePolicy { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string> Tags { get; set; } = [];
}
