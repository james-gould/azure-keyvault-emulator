using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Persistence;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets
{
    public sealed class SecretBundle : TaggedModel, INamedItem
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public required string Name { get; set; }

        [Key]
        [JsonIgnore]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PrimaryId { get; set; }

        [JsonPropertyName("id")]
        public required string SecretIdentifier { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public SecretAttributesModel Attributes { get; set; } = new();
    }
}
