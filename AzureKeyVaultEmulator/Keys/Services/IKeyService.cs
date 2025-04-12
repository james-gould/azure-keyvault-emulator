﻿namespace AzureKeyVaultEmulator.Keys.Services
{
    public interface IKeyService
    {
        KeyResponse? GetKey(string name);
        KeyResponse? GetKey(string name, string version);
        KeyResponse? CreateKey(string name, CreateKeyModel key);

        ValueResponse? GetRandomBytes(int count);

        KeyOperationResult? Encrypt(string name, string version, KeyOperationParameters keyOperationParameters);
        KeyOperationResult? Decrypt(string keyName, string keyVersion, KeyOperationParameters keyOperationParameters);

        ValueResponse? BackupKey(string name);
        KeyResponse? RestoreKey(string jweBody);

        KeyRotationPolicy GetKeyRotationPolicy(string name);
        KeyRotationPolicy UpdateKeyRotationPolicy(string name, KeyRotationAttributes attributes, IEnumerable<LifetimeActions> lifetimeActions);
    }
}
