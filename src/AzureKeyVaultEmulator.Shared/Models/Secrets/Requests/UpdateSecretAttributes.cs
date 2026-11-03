using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets.Requests;

/// <summary>
/// Partial update of a secret version's attributes; null properties leave the stored value unchanged.
/// </summary>
public sealed class UpdateSecretAttributes
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }
}
