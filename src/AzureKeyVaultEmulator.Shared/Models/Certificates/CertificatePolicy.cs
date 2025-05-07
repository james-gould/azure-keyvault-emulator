using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Models.Keys;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

public sealed class CertificatePolicy
{
    [JsonPropertyName("id")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("issuer")]
    public IssuerBundle Issuer { get; set; } = new();

    [JsonPropertyName("attributes")]
    public CertificateAttributesModel CertificateAttributes { get; set; } = new();

    [JsonPropertyName("x509_props")]
    public X509CertificateProperties? CertificateProperties { get; set; } = new();

    [JsonPropertyName("lifetime_actions")]
    public IEnumerable<LifetimeActions> LifetimeActions { get; set; } = [];

    [JsonPropertyName("key_props")]
    public KeyProperties? KeyProperties { get; set; } = new();

    [JsonPropertyName("secret_props")]
    public SecretProperties? SecretProperies { get; set; } = new();
}
