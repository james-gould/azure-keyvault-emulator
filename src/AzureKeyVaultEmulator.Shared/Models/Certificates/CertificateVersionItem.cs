using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

public sealed class CertificateVersionItem : TaggedModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public CertificateAttributesModel Attributes { get; set; } = new();

    [JsonPropertyName("x5t")]
    public string Thumbprint { get; set; } = string.Empty;
}
