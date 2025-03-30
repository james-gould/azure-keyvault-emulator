using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Secrets
{
    public class BackupSecretResult
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}
