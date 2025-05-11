using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

namespace AzureKeyVaultEmulator.Shared.Models.Keys
{
    public class KeyBundle : TaggedModel, INamedItem
    {
        [Key]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid PersistedId { get; set; } = Guid.NewGuid();

        public string PersistedName { get; set; } = string.Empty;

        public string PersistedVersion { get; set; } = string.Empty;

        [JsonPropertyName("key")]
        public required JsonWebKeyModel Key { get; set; }

        [JsonPropertyName("attributes")]
        public KeyAttributesModel Attributes { get; set; } = new();

        [JsonIgnore]
        public bool Deleted { get; set; } = false;
    }
}
