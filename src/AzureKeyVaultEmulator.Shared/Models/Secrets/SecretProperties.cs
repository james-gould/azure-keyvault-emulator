using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets;

public sealed class SecretProperties : IPersistedItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public long PrimaryId { get; set; }

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;
}
