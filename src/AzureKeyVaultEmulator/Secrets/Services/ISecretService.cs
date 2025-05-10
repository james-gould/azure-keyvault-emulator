using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Models.Secrets.Requests;

namespace AzureKeyVaultEmulator.Secrets.Services
{
    public interface ISecretService
    {
        Task<SecretBundle> GetSecretAsync(string name, string version = "");
        Task<SecretBundle> SetSecretAsync(string name, SetSecretRequest requestBody);
        Task<DeletedSecretBundle> DeleteSecretAsync(string name, string version = "");
        Task<ValueModel<string>> BackupSecretAsync(string name);
        Task<SecretBundle> GetDeletedSecretAsync(string name);
        ListResult<SecretBundle> GetDeletedSecrets(int maxVersions = 25, int skipCount = 0);
        ListResult<SecretBundle> GetSecretVersions(string secretName, int maxResults = 25, int skipCount = 0);
        ListResult<SecretBundle> GetSecrets(int maxResults = 25, int skipCount = 0);
        Task PurgeDeletedSecretAsync(string name);
        Task<SecretBundle> RecoverDeletedSecretAsync(string name);
        Task<SecretBundle> RestoreSecretAsync(string encodedName);
        Task<SecretAttributesModel> UpdateSecretAsync(string name, string version, SecretAttributesModel attributes);
    }
}
