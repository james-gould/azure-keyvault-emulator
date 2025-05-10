using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;

namespace AzureKeyVaultEmulator.Shared.Models.Keys;

public sealed class KeyProperties : IPersistedItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public long PrimaryId { get; set; }

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
