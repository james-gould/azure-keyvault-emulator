using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets;

public sealed class SecretBundle : TaggedModel, INamedItem, IAttributedModel<SecretAttributes>
{
    [Key]
    [JsonIgnore]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid PersistedId { get; set; } = Guid.NewGuid();

    public string PersistedName { get; set; } = string.Empty;

    public string PersistedVersion { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public required string SecretIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("managed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Managed { get; set; } = null;

    [JsonPropertyName("attributes")]
    public SecretAttributes Attributes { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public bool Deleted { get; set; } = false;
}
