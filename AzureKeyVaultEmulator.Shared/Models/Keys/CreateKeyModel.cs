using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys
{
    public class CreateKeyModel : ICreateItem
    {
        [JsonPropertyName("kty")]
        [Required]
        public string KeyType { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public KeyAttributesModel KeyAttributes { get; set; } = new();

        [JsonPropertyName("release_policy")]
        public KeyReleasePolicy? keyReleasePolicy { get; set; }

        [JsonPropertyName("crv")]
        public string KeyCurveName { get; set; } = string.Empty;

        [JsonPropertyName("key_ops")]
        public List<string> KeyOperations { get; set; } = [];

        [JsonPropertyName("key_size")]
        public int KeySize { get; set; } = 2048;

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; }
    }
}
