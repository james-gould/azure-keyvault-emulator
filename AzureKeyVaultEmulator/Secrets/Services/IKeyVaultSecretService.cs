﻿using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Secrets.Services
{
    public interface IKeyVaultSecretService
    {
        SecretResponse? Get(string name, string version = "");
        SecretResponse? SetSecret(string name, SetSecretModel requestBody);
        DeletedSecretBundle? DeleteSecret(string name, string version = "");
        BackupSecretResult? BackupSecret(string name);
        SecretResponse? GetDeletedSecret(string name);
        ListResult<SecretResponse> GetDeletedSecrets(int maxVersions = 25, int skipCount = 0);
        IEnumerable<string> GetSecretVersions(string secretName, int maxResults = 25, int skipCount = 0);
        IEnumerable<SecretResponse?> GetSecrets(int maxResults = 25, int skipCount = 0);
        void PurgeDeletedSecret(string name);
        void RecoverDeletedSecret(string name);
        BackupSecretResult? RestoreSecret(string encodedName);
        void UpdateSecret(string name, string version, SecretAttributesModel? attributes = null, string contentType = "", object? tags = null);
    }
}
