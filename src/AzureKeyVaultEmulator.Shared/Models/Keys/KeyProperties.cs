using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

namespace AzureKeyVaultEmulator.Shared.Models.Keys;

public sealed class KeyProperties : IPersistedItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Guid PersistedId { get; set; } = Guid.NewGuid();

    [JsonPropertyName("crv")]
    public string JsonWebKeyCurveName { get; set; } = string.Empty;

    [JsonPropertyName("exportable")]
    public static bool Exportable => true;

    [JsonPropertyName("key_size")]
    public int KeySize { get; set; } = 2048;

    [JsonPropertyName("kty")]
    public string JsonWebKeyType { get; set; } = string.Empty;

    [JsonPropertyName("reuse_key")]
    public bool ReuseKey { get; set; }
}
