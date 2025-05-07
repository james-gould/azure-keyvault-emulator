using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;

public sealed class ImportCertificateRequest : ValueModel<string>
{
    [JsonPropertyName("attributes")]
    public CertificateAttributesModel Attributes { get; set; } = new();

    [JsonPropertyName("policy")]
    public CertificatePolicy Policy { get; set; } = new();

    [JsonPropertyName("pwd")]
    public string? Password { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public Dictionary<string, string> Tags { get; set; } = [];
}
