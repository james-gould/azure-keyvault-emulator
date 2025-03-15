using System;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Shared.Models;

namespace AzureKeyVaultEmulator.Secrets.Models
{
    public sealed class SecretResponse : ResponseBase
    {
        [JsonPropertyName("id")]
        public Uri? Id { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public SecretAttributesModel Attributes { get; set; } = new();
    }
}