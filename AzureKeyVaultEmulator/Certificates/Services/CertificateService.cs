using AzureKeyVaultEmulator.Certificates.Factories;
using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Certificates.Services;

public sealed class CertificateService(IHttpContextAccessor httpContextAccessor) : ICertificateService
{
    private static readonly ConcurrentDictionary<string, CertificateBundle> _certs = [];
    private static readonly ConcurrentDictionary<string, KeyProperties> _backingKeys = [];
    private static readonly ConcurrentDictionary<string, SecretProperties> _backingSecrets = [];

    public CertificateOperation CreateCertificate(
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

        var certIdentifier = httpContextAccessor.BuildIdentifierUri(name, OperationConstants.Pending, "certificates");

        var bundle = new CertificateBundle
        {
            CertificateIdentifier = certIdentifier,
            Attributes = attributes,
            CertificateName = name,
            VaultUri = new Uri(AuthConstants.EmulatorUri),
            CertificatePolicy = GetPolicy(policy, certIdentifier.ToString(), attributes),
            X509Thumbprint = certificate.Thumbprint,
            CertificateContents = Convert.ToBase64String(certificate.RawData)
        };

        _certs.SafeAddOrUpdate(name.GetCacheId(), bundle);

        return new CertificateOperation(certIdentifier.ToString(), name);
    }

    public CertificateOperation GetPendingCertificate(string name)
    {
        var cert = _certs.SafeGet(name.GetCacheId());

        return new CertificateOperation(cert.CertificateIdentifier.ToString(), name);
    }

    public CertificateBundle GetCertificate(string name)
    {
        return _certs.SafeGet(name.GetCacheId());
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
