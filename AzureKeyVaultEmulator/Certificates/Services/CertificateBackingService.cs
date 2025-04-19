using AzureKeyVaultEmulator.Keys.Services;
using AzureKeyVaultEmulator.Secrets.Services;
using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Certificates.Services;

public sealed class CertificateBackingService(IKeyService keyService, ISecretService secretService)
    : ICertificateBackingService
{
    // { issuerName, issuerBundle }
    private static readonly ConcurrentDictionary<string, IssuerBundle> _issuers = new();

    // { certName, IssuerBundle } 
    private static readonly ConcurrentDictionary<string, IssuerBundle> _certificateIssuers = new();

    public IssuerBundle GetIssuer(string name)
    {
        return _issuers.SafeGet(name.GetCacheId());
    }

    public (KeyBundle backingKey, SecretBundle backingSecret) GetBackingComponents(string certName, CertificatePolicy? policy = null)
    {
        var keySize = policy?.KeyProperties?.KeySize ?? 2048;
        var keyType = !string.IsNullOrEmpty(policy?.KeyProperties?.JsonWebKeyType) ? policy.KeyProperties.JsonWebKeyType : SupportedKeyTypes.RSA;

        var backingKey = CreateBackingKey(certName, keySize, keyType);

        var contentType = policy?.SecretProperies?.ContentType ?? "application/unknown";
        var backingSecret = CreateBackingSecret(certName, contentType);

        return (backingKey, backingSecret);
    }

    public IssuerBundle PersistIssuerConfig(string name, IssuerBundle bundle)
    {
        // Name is passed as a route arg, not set in model...
        bundle.IssuerName = name;

        _issuers.SafeAddOrUpdate(name.GetCacheId(), bundle);

        return bundle;
    }

    public IssuerBundle UpdateIssuerAgainstCertificate(string certName, IssuerBundle bundle)
    {
        _certificateIssuers.SafeAddOrUpdate(certName, bundle);

        return bundle;
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
