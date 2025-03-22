using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets
{
    public class DeletedSecretBundle : ResponseBase
    {
        [JsonPropertyName("attributes")]
        public SecretAttributesModel? Attributes { get; set; }

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("deletedDate")]
        public long DeletedDate { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds();

        [JsonPropertyName("id")]
        public string SecretId { get; set; } = string.Empty;

        [JsonPropertyName("kid")]
        public string Kid { get; set; } = string.Empty;

        [JsonPropertyName("managed")]
        public bool Managed { get; set; }

        [JsonPropertyName("scheduledPurgeDate")]
        public long ScheduledPurgeDateUTC { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds();

        public string Value { get; set; } = string.Empty;
    }
}
