using AzureKeyVaultEmulator.Keys.Services;
using AzureKeyVaultEmulator.Secrets.Services;
using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Certificates.Services;

public sealed class CertificateBackingService(IKeyService keyService, ISecretService secretService)
    : ICertificateBackingService
{
    // { issuerName, issuerBundle }
    private static readonly ConcurrentDictionary<string, IssuerBundle> _issuers = new();

    // { certName, IssuerBundle } 
    private static readonly ConcurrentDictionary<string, IssuerBundle> _certificateIssuers = new();

    // This seems basically unused outside of key vault, the docs, SDK nor portal make use of it?
    // Might be an artefact or maybe it's just really obvious and I am stupid.
    private CertificateContacts _certContacts = new();

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

    public IssuerBundle DeleteIssuer(string issuerName)
    {
        ArgumentException.ThrowIfNullOrEmpty(issuerName);

        var cacheId = issuerName.GetCacheId();

        var bundle = _issuers.SafeGet(cacheId);

        _issuers.SafeRemove(cacheId);

        return bundle;
    }

    public IssuerBundle PersistIssuerConfig(string issuerName, IssuerBundle bundle)
    {
        // Name is passed as a route arg from the SDK, not set in model
        // The response model in the SDK has a Name property though, so it needs to be set.
        bundle.IssuerName = issuerName;

        _issuers.SafeAddOrUpdate(issuerName.GetCacheId(), bundle);

        return bundle;
    }

    public IssuerBundle AllocateIssuerToCertificate(string certName, IssuerBundle bundle)
    {
        _certificateIssuers.SafeAddOrUpdate(certName, bundle);

        return bundle;
    }

    public IssuerBundle UpdateCertificateIssuer(string issuerName, IssuerBundle bundle)
    {
        ArgumentException.ThrowIfNullOrEmpty(issuerName);
        ArgumentNullException.ThrowIfNull(bundle);

        _issuers.SafeAddOrUpdate(issuerName, bundle);

        return bundle;
    }

    public CertificateContacts SetContactInformation(SetContactsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _certContacts.Contacts = request.Contacts;

        return _certContacts;
    }

    public CertificateContacts DeleteCertificateContacts()
    {
        return _certContacts = new();
    }

    public CertificateContacts GetCertificateContacts() => _certContacts;

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
