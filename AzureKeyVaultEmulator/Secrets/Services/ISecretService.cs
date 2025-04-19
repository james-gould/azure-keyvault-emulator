using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Secrets.Services
{
    public interface ISecretService
    {
        SecretBundle GetSecret(string name, string version = "");
        SecretBundle SetSecret(string name, SetSecretModel requestBody);
        DeletedSecretBundle? DeleteSecret(string name, string version = "");
        ValueModel<string>? BackupSecret(string name);
        SecretBundle? GetDeletedSecret(string name);
        ListResult<SecretBundle> GetDeletedSecrets(int maxVersions = 25, int skipCount = 0);
        ListResult<SecretBundle> GetSecretVersions(string secretName, int maxResults = 25, int skipCount = 0);
        ListResult<SecretBundle> GetSecrets(int maxResults = 25, int skipCount = 0);
        void PurgeDeletedSecret(string name);
        SecretBundle? RecoverDeletedSecret(string name);
        SecretBundle? RestoreSecret(string encodedName);
        SecretAttributesModel UpdateSecret(string name, string version, SecretAttributesModel attributes);
    }
}
