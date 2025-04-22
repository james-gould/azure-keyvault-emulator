using System.Text.Json.Serialization;
using Azure.Security.KeyVault.Certificates;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

public sealed class X509CertificateProperties
{
    [JsonPropertyName("ekus")]
    public IEnumerable<string> EnhancedKeyUsage { get; set; } = [];

    [JsonPropertyName("key_usage")]
    public IEnumerable<CertificateKeyUsage> KeyUsage { get; set; } = [];

    [JsonPropertyName("sans")]
    public SubjectAlternativeNames SubjectAlternativeNames { get; set; } = new();

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("validity_months")]
    public int ValidityMonths { get; set; } = 0;
}

public sealed class SubjectAlternativeNames
{
    [JsonPropertyName("dns_names")]
    public IEnumerable<string> DnsNames { get; set; } = [];

    [JsonPropertyName("emails")]
    public IEnumerable<string> Emails { get; set; } = [];

    [JsonPropertyName("upns")]
    public IEnumerable<string> Upns { get; set; } = [];
}
