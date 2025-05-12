using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

namespace AzureKeyVaultEmulator.Shared.Models
{
    public class AttributeBase : IPersistedItem
    {
        public AttributeBase()
        {
            var now = DateTimeOffset.Now;

            Created = now.ToUnixTimeSeconds();
            Updated = now.ToUnixTimeSeconds();
            NotBefore = now.ToUnixTimeSeconds();
            Expiration = now.AddDays(365).ToUnixTimeSeconds();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)] 
        public Guid PersistedId { get; set; } = Guid.NewGuid();

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("exp")]
        public long Expiration { get; set; }

        [JsonPropertyName("nbf")]
        public long NotBefore { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("updated")]
        public long Updated { get; set; }

        [JsonPropertyName("recoveryLevel")]
        public string RecoveryLevel = DeletionRecoveryLevel.Purgable.ToString();

        public void Update() => Updated = DateTimeOffset.Now.ToUnixTimeSeconds();
    }
}
