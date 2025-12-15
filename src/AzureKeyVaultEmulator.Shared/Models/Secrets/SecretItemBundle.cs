using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets;

public class SecretItemBundle : TaggedModel
{
    [JsonPropertyName("attributes")]
    public SecretAttributes SecretAttributes { get; set; } = new();

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("contentType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentType { get; set; } = null;

    [JsonPropertyName("managed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Managed { get; set; } = null;
}
