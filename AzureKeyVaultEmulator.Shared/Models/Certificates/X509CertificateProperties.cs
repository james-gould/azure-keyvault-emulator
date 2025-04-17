using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

public sealed class X509CertificateProperties
{
    [JsonPropertyName("dns_names")]
    public IEnumerable<string> DnsNames { get; set; } = [];

    [JsonPropertyName("emails")]
    public IEnumerable<string> Emails { get; set; } = [];

    [JsonPropertyName("upns")]
    public IEnumerable<string> Upns { get; set; } = [];
}
