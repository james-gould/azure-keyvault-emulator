using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets
{
    public sealed class SecretBundle : TaggedModel, INamedItem
    {
        [Key]
        [JsonIgnore]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid PersistedId { get; set; } = Guid.NewGuid();

        public string PersistedName { get; set; } = string.Empty;

        public string PersistedVersion { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public bool Deleted { get; set; } = false;

        [JsonPropertyName("id")]
        public required string SecretIdentifier { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public SecretAttributesModel Attributes { get; set; } = new();
    }
}
