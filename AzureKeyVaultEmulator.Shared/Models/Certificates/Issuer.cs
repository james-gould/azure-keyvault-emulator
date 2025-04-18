using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Certificates;

public sealed class Issuer
{

    [JsonPropertyName("name")]
    public string IssuerName { get; set; } = string.Empty;
}
