using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;
using System.Text.Json;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

public sealed class X509CertificateProperties : IPersistedItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Guid PersistedId { get; set; } = Guid.NewGuid();

    public string BackingEnhancedUsage { get; set; } = "[]";

    [JsonPropertyName("ekus")]
    [NotMapped]
    public IEnumerable<string> EnhancedKeyUsage
    {
        get => JsonSerializer.Deserialize<IEnumerable<string>>(BackingEnhancedUsage) ?? [];
        set => BackingEnhancedUsage = JsonSerializer.Serialize(value);
    }

    [JsonPropertyName("emulator_keyUsage")]
    public string BackingKeyUsage { get; set; } = "[]";

    [JsonPropertyName("key_usage")]
    [NotMapped]
    public IEnumerable<CertificateKeyUsage> KeyUsage
    {
        get => JsonSerializer.Deserialize<IEnumerable<CertificateKeyUsage>>(BackingKeyUsage) ?? [];
        set => BackingKeyUsage = JsonSerializer.Serialize(value);
    }

    [JsonPropertyName("sans")]
    public SubjectAlternativeNames SubjectAlternativeNames { get; set; } = new();

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("validity_months")]
    public int ValidityMonths { get; set; } = 0;
}

public sealed class SubjectAlternativeNames : IPersistedItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Guid PersistedId { get; set; } = Guid.NewGuid();

    public string BackingDns { get; set; } = "[]";

    [JsonPropertyName("dns_names")]
    [NotMapped]
    public IEnumerable<string> DnsNames
    {
        get => JsonSerializer.Deserialize<IEnumerable<string>>(BackingDns) ?? [];
        set => BackingDns = JsonSerializer.Serialize(value);
    }

    public string BackingEmails { get; set; } = "[]";

    [JsonPropertyName("emails")]
    [NotMapped]
    public IEnumerable<string> Emails
    {
        get => JsonSerializer.Deserialize<IEnumerable<string>>(BackingEmails) ?? [];
        set => BackingEmails = JsonSerializer.Serialize(value);
    }

    public string BackingUpns { get; set; } = "[]";

    [JsonPropertyName("upns")]
    [NotMapped]
    public IEnumerable<string> Upns
    {
        get => JsonSerializer.Deserialize<IEnumerable<string>>(BackingUpns) ?? [];
        set => BackingUpns = JsonSerializer.Serialize(value);
    }
}
