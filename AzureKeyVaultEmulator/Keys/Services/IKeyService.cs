using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Keys.Services
{
    public interface IKeyService
    {
        KeyBundle? GetKey(string name);
        KeyBundle? GetKey(string name, string version);
        KeyBundle? CreateKey(string name, CreateKeyModel key);
        KeyAttributesModel? UpdateKey(string name, string version, KeyAttributesModel attributes);
        KeyBundle? RotateKey(string name, string version);

        ValueResponse? GetRandomBytes(int count);

        KeyOperationResult? Encrypt(string name, string version, KeyOperationParameters keyOperationParameters);
        KeyOperationResult? Decrypt(string keyName, string keyVersion, KeyOperationParameters keyOperationParameters);

        ValueResponse? BackupKey(string name);
        KeyBundle? RestoreKey(string jweBody);

        KeyRotationPolicy GetKeyRotationPolicy(string name);
        KeyRotationPolicy UpdateKeyRotationPolicy(string name, KeyRotationAttributes attributes, IEnumerable<LifetimeActions> lifetimeActions);

        ListResult<KeyBundle> GetKeys(int maxResults = 25, int skipCount = 25);
        ListResult<KeyBundle> GetKeyVersions(string name, int maxResults = 25, int skipCount = 25);

        ValueResponse ReleaseKey(string name, string version);
        KeyBundle ImportKey(string name, JsonWebKey key, KeyAttributesModel attributes, Dictionary<string, string> tags);
        KeyOperationResult SignWithKey(string name, string version, string algo, string value);
        bool VerifyDigest(string name, string version, string digest, string signature);

        KeyOperationResult WrapKey(string name, string version, KeyOperationParameters para);
        KeyOperationResult UnwrapKey(string name, string version, KeyOperationParameters para);

        DeletedKeyBundle DeleteKey(string name);
        KeyBundle GetDeletedKey(string name);
        ListResult<KeyBundle> GetDeletedKeys(int maxResults = 25, int skipCount = 25);
        void PurgeDeletedKey(string name);
        KeyBundle RecoverDeletedKey(string name);
    }
}
