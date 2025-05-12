using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Constants;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;

public sealed class CertificateContacts : INamedItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Guid PersistedId { get; set; } = Guid.NewGuid();

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string PersistedVersion { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string PersistedName { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public bool Deleted { get; set; } = false;

    [JsonPropertyName("id")]
    public string Id { get; } = $"{AuthConstants.EmulatorUri}/certificates/contacts";

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string BackingContacts { get; set; } = "[]";

    [JsonPropertyName("contacts")]
    [NotMapped]
    public IEnumerable<KeyVaultContact> Contacts
    {
        get => JsonSerializer.Deserialize<IEnumerable<KeyVaultContact>>(BackingContacts) ?? [];
        set => BackingContacts = JsonSerializer.Serialize(value);
    }
}
