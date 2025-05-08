using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models
{
    public class TaggedModel
    {
        [NotMapped]
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; } = [];
    }
}
