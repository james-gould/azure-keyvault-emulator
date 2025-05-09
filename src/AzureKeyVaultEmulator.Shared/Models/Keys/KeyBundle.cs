using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys
{
    public class KeyBundle : TaggedModel
    {
        [Key]
        [JsonIgnore]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PrimaryId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("key")]
        public required JsonWebKeyModel Key { get; set; }

        [JsonPropertyName("attributes")]
        public KeyAttributesModel Attributes { get; set; } = new();
    }
}
