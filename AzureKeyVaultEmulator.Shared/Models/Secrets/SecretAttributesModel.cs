namespace AzureKeyVaultEmulator.Shared.Models.Secrets
{
    public class SecretAttributesModel : AttributeBase
    {
        public SecretAttributesModel()
        {
            var now = DateTimeOffset.Now;

            NotBefore = now.ToUnixTimeSeconds();
            Created = now.ToUnixTimeSeconds();
            Expiration = now.AddDays(30).ToUnixTimeSeconds();
        }
    }
}
