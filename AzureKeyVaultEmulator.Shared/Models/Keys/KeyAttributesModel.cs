using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Shared.Models.Keys
{
    public class KeyAttributesModel : AttributeBase
    {
        public KeyAttributesModel()
        {
            var now = DateTimeOffset.Now;

            NotBefore = now.ToUnixTimeSeconds();
            Created = now.ToUnixTimeSeconds();
            Expiration = now.AddDays(30).ToUnixTimeSeconds();
        }

        [JsonPropertyName("recoverableDays")]
        public int RecoverableDays { get; set; }
    }
}
