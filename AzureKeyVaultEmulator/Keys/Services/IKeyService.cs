using AzureKeyVaultEmulator.Shared.Models.Shared;

namespace AzureKeyVaultEmulator.Keys.Services
{
    public interface IKeyService
    {
        KeyResponse? Get(string name);
        KeyResponse? Get(string name, string version);
        KeyResponse? CreateKey(string name, CreateKeyModel key);

        ValueResponse? GetRandomBytes(int count);

        KeyOperationResult? Encrypt(string name, string version, KeyOperationParameters keyOperationParameters);
        KeyOperationResult? Decrypt(string keyName, string keyVersion, KeyOperationParameters keyOperationParameters);

        ValueResponse? BackupKey(string name);
        KeyResponse? RestoreKey(string jweBody);
    }
}
