using AzureKeyVaultEmulator.Certificates.Factories;
using AzureKeyVaultEmulator.Shared.Models.Certificates;

namespace AzureKeyVaultEmulator.Certificates.Services;

public sealed class CertificateService(
    IHttpContextAccessor httpContextAccessor,
    ICertificateBackingService backingService)
    : ICertificateService
{
    private static readonly ConcurrentDictionary<string, CertificateBundle> _certs = [];

    public CertificateOperation CreateCertificate(
        string name,
        CertificateAttributesModel attributes,
        CertificatePolicy? policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(attributes);

        var certificate = X509CertificateFactory.BuildX509Certificate(name, policy);

        var (backingKey, backingSecret) = backingService.GetBackingComponents(name, policy);

        var version = Guid.NewGuid().Neat();

        attributes.Version = version;
        attributes.NotBefore = certificate.NotBefore.ToUnixTimeSeconds();
        attributes.Expiration = certificate.NotAfter.ToUnixTimeSeconds();

        var certIdentifier = httpContextAccessor.BuildIdentifierUri(name, version, "certificates");

        var bundle = new CertificateBundle
        {
            CertificateIdentifier = certIdentifier,
            Attributes = attributes,
            CertificateName = name,
            VaultUri = new Uri(AuthConstants.EmulatorUri),
            CertificatePolicy = GetPolicy(policy, certIdentifier.ToString(), attributes),
            X509Thumbprint = certificate.Thumbprint,
            CertificateContents = Convert.ToBase64String(certificate.RawData),
            SecretId = backingSecret.Id.ToString(),
            KeyId = backingKey.Key.KeyIdentifier
        };

        _certs.SafeAddOrUpdate(name.GetCacheId(), bundle);
        _certs.SafeAddOrUpdate(name.GetCacheId(version), bundle);

        return new CertificateOperation(certIdentifier.ToString(), name);
    }

    public CertificateBundle GetCertificate(string name, string version = "")
    {
        return _certs.SafeGet(name.GetCacheId(version));
    }

    public CertificatePolicy UpdateCertificatePolicy(string name, CertificatePolicy policy)
    {
        var cert = _certs.SafeGet(name.GetCacheId());

        cert.CertificatePolicy = policy;

        _certs.SafeAddOrUpdate(name, cert);

        return policy;
    }

    public CertificateOperation GetPendingCertificate(string name)
    {
        var cert = _certs.SafeGet(name.GetCacheId());

        return new CertificateOperation(cert.CertificateIdentifier.ToString(), name);
    }

    private static CertificatePolicy GetPolicy(
        CertificatePolicy? policy,
        string identifier,
        CertificateAttributesModel attributes)
    {
        policy ??= new();

        policy.Identifier = identifier;
        policy.CertificateAttributes ??= attributes;

        return policy;
    }
}
