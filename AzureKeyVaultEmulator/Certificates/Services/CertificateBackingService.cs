using AzureKeyVaultEmulator.Keys.Services;
using AzureKeyVaultEmulator.Secrets.Services;
using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Certificates.Services;

public sealed class CertificateBackingService(IKeyService keyService, ISecretService secretService)
    : ICertificateBackingService
{
    public (KeyBundle backingKey, SecretBundle backingSecret) GetBackingComponents(string certName, CertificatePolicy? policy = null)
    {
        var keySize = policy?.KeyProperties?.KeySize ?? 2048;
        var keyType = !string.IsNullOrEmpty(policy?.KeyProperties?.JsonWebKeyType) ? policy.KeyProperties.JsonWebKeyType : SupportedKeyTypes.RSA;

        var backingKey = CreateBackingKey(certName, keySize, keyType);

        var contentType = policy?.SecretProperies?.ContentType ?? "application/unknown";
        var backingSecret = CreateBackingSecret(certName, contentType);

        return (backingKey, backingSecret);
    }

    private KeyBundle CreateBackingKey(
        string certName,
        int keySize,
        string keyType)
    {
        return keyService.CreateKey(certName, new CreateKeyModel { KeySize = keySize, KeyType = keyType });
    }

    private SecretBundle CreateBackingSecret(string certName, string contentType)
    {
        return secretService
            .SetSecret(certName, new SetSecretModel
            {
                Value = Guid.NewGuid().Neat(),
                SecretAttributes = new() { ContentType = contentType }
            });
    }
}
