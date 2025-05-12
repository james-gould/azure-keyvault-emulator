using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models
{
    public class TaggedModel
    {
        [NotMapped]
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; } = [];

        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        [Column("Tags")]
        public string TagsSerialized
        {
            get => JsonSerializer.Serialize(Tags);
            set => Tags = string.IsNullOrEmpty(value)
                ? []
                : JsonSerializer.Deserialize<Dictionary<string, string>>(value) ?? [];
        }
    }
}
