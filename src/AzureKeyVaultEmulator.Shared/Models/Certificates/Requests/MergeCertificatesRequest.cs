using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;

public sealed class MergeCertificatesRequest : TaggedModel
{
    [JsonPropertyName("x5c")]
    public IEnumerable<string> Certificates { get; set; } = [];

    [JsonPropertyName("attributes")]
    public CertificateAttributesModel Attributes { get; set; } = new();
}
