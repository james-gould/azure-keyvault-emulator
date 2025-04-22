using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models;

public class DeletedBundle<TAttributes> : TaggedModel where TAttributes : AttributeBase
{
    [JsonPropertyName("name")]
    public required string Name { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public TAttributes? Attributes { get; set; }

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("kid")]
    public string Kid { get; set; } = string.Empty;

    [JsonPropertyName("managed")]
    public bool Managed { get; set; }

    [JsonPropertyName("recoveryId")]
    public required string RecoveryId { get; set; }

    [JsonPropertyName("deletedDate")]
    public long DeletedDate { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds();

    [JsonPropertyName("scheduledPurgeDate")]
    public long ScheduledPurgeDateUTC { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds();
}
