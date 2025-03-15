using AzureKeyVaultEmulator.Shared.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Keys.Models
{
    public class CreateKeyModel : ICreateItem
    {
        [JsonPropertyName("kty")]
        [Required]
        public string KeyType { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public KeyAttributesModel KeyAttributes { get; set; } = new();

        [JsonPropertyName("crv")]
        public string KeyCurveName { get; set; } = string.Empty;

        [JsonPropertyName("key_ops")]
        public List<string> KeyOperations { get; set; } = [];

        [JsonPropertyName("key_size")]
        public int? KeySize { get; set; }

        [JsonPropertyName("tags")]
        public object? Tags { get; set; }
    }
}
