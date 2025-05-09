using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Keys.Services
{
    public interface IKeyService
    {
        Task<KeyBundle> GetKeyAsync(string name);
        Task<KeyBundle> GetKeyAsync(string name, string version);
        Task<KeyBundle> CreateKeyAsync(string name, CreateKeyModel key);
        Task<KeyAttributesModel?> UpdateKeyAsync(string name, string version, KeyAttributesModel attributes, Dictionary<string, string> tags);
        Task<KeyBundle?> RotateKey(string name, string version);

        ValueModel<string> GetRandomBytes(int count);

        Task<KeyOperationResult?> EncryptAsync(string name, string version, KeyOperationParameters keyOperationParameters);
        Task<KeyOperationResult?> DecryptAsync(string keyName, string keyVersion, KeyOperationParameters keyOperationParameters);

        Task<ValueModel<string>?> BackupKeyAsync(string name);
        KeyBundle RestoreKey(string jweBody);

        KeyRotationPolicy GetKeyRotationPolicy(string name);
        Task<KeyRotationPolicy> UpdateKeyRotationPolicyAsync(string name, KeyRotationAttributes attributes, IEnumerable<LifetimeActions> lifetimeActions);

        ListResult<KeyItemBundle> GetKeys(int maxResults = 25, int skipCount = 25);
        ListResult<KeyItemBundle> GetKeyVersions(string name, int maxResults = 25, int skipCount = 25);

        Task<ValueModel<string>> ReleaseKeyAsync(string name, string version);
        Task<KeyBundle> ImportKeyAsync(string name, JsonWebKey key, KeyAttributesModel attributes, Dictionary<string, string> tags);
        Task<KeyOperationResult> SignWithKeyAsync(string name, string version, string algo, string value);
        Task<ValueModel<bool>> VerifyDigestAsync(string name, string version, string digest, string signature);

        Task<KeyOperationResult> WrapKeyAsync(string name, string version, KeyOperationParameters para);
        Task<KeyOperationResult> UnwrapKeyAsync(string name, string version, KeyOperationParameters para);

        Task<DeletedKeyBundle> DeleteKeyAsync(string name);
        Task<KeyBundle> GetDeletedKeyAsync(string name);
        ListResult<KeyBundle> GetDeletedKeys(int maxResults = 25, int skipCount = 25);
        Task PurgeDeletedKey(string name);
        Task<KeyBundle> RecoverDeletedKeyAsync(string name);
    }
}
