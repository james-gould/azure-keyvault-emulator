using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Models.Keys;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

public sealed class CertificatePolicy
{
    [JsonPropertyName("id")]
    public required string Identifier { get; set; }

    [JsonPropertyName("attributes")]
    public CertificateAttributesModel? CertificateAttributes { get; set; }

    [JsonPropertyName("lifetime_actions")]
    public IEnumerable<LifetimeActions> LifetimeActions { get; set; } = [];

    [JsonPropertyName("key_props")]
    public KeyProperties? KeyProperties { get; set; }

    [JsonPropertyName("secret_props")]
    public SecretProperties? SecretProperies { get; set; }

    [JsonPropertyName("x509_props")]
    public X509CertificateProperties? CertificateProperties { get; set; }
}
