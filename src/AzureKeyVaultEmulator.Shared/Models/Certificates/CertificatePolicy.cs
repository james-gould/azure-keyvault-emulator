using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Models.Keys;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

public sealed class CertificatePolicy : INamedItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public long PrimaryId { get; set; }

    public string PersistedName { get; set; } = string.Empty;

    public string PersistedVersion { get; set; } = string.Empty;

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

    public string BackingLifetimeActions = "[]";

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
}
