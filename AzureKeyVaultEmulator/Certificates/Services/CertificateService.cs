using AzureKeyVaultEmulator.Certificates.Factories;
using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Certificates.Services;

public sealed class CertificateService(IHttpContextAccessor httpContextAccessor) : ICertificateService
{
    private static readonly ConcurrentDictionary<string, CertificateBundle> _certs = [];
    private static readonly ConcurrentDictionary<string, KeyProperties> _backingKeys = [];
    private static readonly ConcurrentDictionary<string, SecretProperties> _backingSecrets = [];

    public CertificateBundle CreateCertificate(
        string name,
        CertificateAttributesModel attributes,
        CertificatePolicy? policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(attributes);

        if(policy?.KeyProperties is not null)
            _backingKeys.TryAdd(name, policy.KeyProperties);

        if(policy?.SecretProperies is not null)
            _backingSecrets.TryAdd(name, policy.SecretProperies);

        var certificate = X509CertificateFactory.BuildX509Certificate(name, policy);

        attributes.NotBefore = certificate.NotBefore.ToUnixTimeSeconds();
        attributes.Expiration = certificate.NotAfter.ToUnixTimeSeconds();

        var certType = attributes.ContentType.FromApplicationContentType();
        var certIdentifier = httpContextAccessor.BuildIdentifierUri(name, "", "certificates").ToString();

        var bundle = new CertificateBundle(certType)
        {
            CertificateIdentifier = certIdentifier,
            Attributes = attributes,
            CertificatePolicy = GetPolicy(policy, certIdentifier),
            Thumbprint = certificate.Thumbprint,
            CertificateContents = EncodingUtils.Base64UrlEncode(certificate.RawData)
        };

        _certs.TryAdd(name, bundle);

        return bundle;
    }

    public CertificateBundle GetCertificate(string name, string version)
    {
        return _certs.SafeGet(name.GetCacheId(version));
    }

    // Feels janky
    private static CertificatePolicy GetPolicy(CertificatePolicy? policy, string identifier)
    {
        if (policy is not null)
            policy.Identifier = identifier;
        else
            policy = new() { Identifier  = identifier };

        return policy;
    }
}
