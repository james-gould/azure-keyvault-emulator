using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Models.Keys;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;
using AzureKeyVaultEmulator.Shared.Utilities;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

public sealed class CertificatePolicy : INamedItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Guid PersistedId { get; set; } = Guid.NewGuid();

    public string PersistedName { get; set; } = Guid.NewGuid().Neat();

    public string PersistedVersion { get; set; } = Guid.NewGuid().Neat();

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Guid ParentCertificateId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Guid IssuerId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public bool Deleted { get; set; } = false;

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

    public string BackingLifetimeActions { get; set; } = "[]";

    [JsonPropertyName("lifetime_actions")]
    [NotMapped]
    public IEnumerable<LifetimeActions> LifetimeActions
    {
        get => JsonSerializer.Deserialize<IEnumerable<LifetimeActions>>(BackingLifetimeActions) ?? [];
    }

    [JsonPropertyName("key_props")]
    public KeyProperties? KeyProperties { get; set; } = new();

    [JsonPropertyName("secret_props")]
    public SecretProperties? SecretProperies { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public CertificateBundle? CertificateBundle { get; set; } = null;
}
