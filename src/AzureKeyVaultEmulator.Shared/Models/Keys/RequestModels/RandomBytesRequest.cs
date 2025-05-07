using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys.RequestModels;

public sealed class RandomBytesRequest
{
    [JsonPropertyName("count")]
    public required int Count { get; set; }
}
