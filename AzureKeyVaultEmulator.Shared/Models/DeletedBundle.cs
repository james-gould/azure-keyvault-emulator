using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models;

public class DeletedBundle<TAttributes> : ResponseBase where TAttributes : AttributeBase
{
    [JsonPropertyName("attributes")]
    public TAttributes? Attributes { get; set; }

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("deletedDate")]
    public long DeletedDate { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds();

    [JsonPropertyName("kid")]
    public string Kid { get; set; } = string.Empty;

    [JsonPropertyName("managed")]
    public bool Managed { get; set; }

    [JsonPropertyName("scheduledPurgeDate")]
    public long ScheduledPurgeDateUTC { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds();
}
