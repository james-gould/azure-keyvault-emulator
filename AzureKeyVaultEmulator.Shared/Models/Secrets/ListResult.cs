using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets
{
    public class ListResult<TResponse> where TResponse : ResponseBase
    {
        [JsonPropertyName("nextLink")]
        public string NextLink { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public IEnumerable<TResponse?> Values { get; set; } = [];
    }
}
