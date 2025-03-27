namespace AzureKeyVaultEmulator.Keys.Services
{
    public interface IKeyService
    {
        KeyResponse? Get(string name);
        KeyResponse? Get(string name, string version);
        KeyResponse? CreateKey(string name, CreateKeyModel key);

        KeyOperationResult? Encrypt(string name, string version, KeyOperationParameters keyOperationParameters);
        KeyOperationResult? Decrypt(string keyName, string keyVersion, KeyOperationParameters keyOperationParameters);
    }
}
