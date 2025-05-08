using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys
{
    public class KeyBundle : TaggedModel
    {
        [JsonIgnore]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PrimaryId { get; set; }

        [JsonPropertyName("key")]
        public JsonWebKeyModel Key { get; set; } = new();

        [JsonPropertyName("attributes")]
        public KeyAttributesModel Attributes { get; set; } = new();
    }
}
