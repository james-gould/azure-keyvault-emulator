using AzureKeyVaultEmulator.Certificates.Factories;
using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Certificates.Services;

public sealed class CertificateService(
    IHttpContextAccessor httpContextAccessor,
    ICertificateBackingService backingService,
    IEncryptionService encryptionService,
    ITokenService tokenService)
    : ICertificateService
{
    private static readonly ConcurrentDictionary<string, CertificateBundle> _certs = [];
    private static readonly ConcurrentDictionary<string, DeletedCertificateBundle> _deletedCerts = [];

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
            CertificatePolicy = UpdateNullablePolicy(policy, certIdentifier, attributes),
            X509Thumbprint = certificate.Thumbprint,
            CertificateContents = Convert.ToBase64String(certificate.RawData),
            SecretId = backingSecret.SecretIdentifier,
            KeyId = backingKey.Key.KeyIdentifier,

            FullCertificate = certificate
        };

        _certs.SafeAddOrUpdate(name.GetCacheId(), bundle);
        _certs.SafeAddOrUpdate(name.GetCacheId(version), bundle);

        return new CertificateOperation(certIdentifier, name);
    }

    public CertificateBundle GetCertificate(string name, string version = "")
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _certs.SafeGet(name.GetCacheId(version));
    }

    public CertificatePolicy GetCertificatePolicy(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = _certs.SafeGet(name.GetCacheId());

        return cert.CertificatePolicy
            ?? throw new InvalidOperationException($"Found certificate {cert.CertificateName} but the associated policy was null.");
    }

    public CertificatePolicy UpdateCertificatePolicy(string name, CertificatePolicy policy)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cacheId = name.GetCacheId();

        var cert = _certs.SafeGet(cacheId);

        cert.CertificatePolicy = policy;

        _certs.SafeAddOrUpdate(name, cert);

        backingService.AllocateIssuerToCertificate(cacheId, policy.Issuer);

        return policy;
    }

    public CertificateOperation GetPendingCertificate(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = _certs.SafeGet(name.GetCacheId());

        return new CertificateOperation(cert.CertificateIdentifier, name);
    }

    public IssuerBundle GetCertificateIssuer(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return backingService.GetIssuer(name);
    }

    public ValueModel<string> BackupCertificate(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = _certs.SafeGet(name.GetCacheId());

        return new ValueModel<string>
        {
            Value = encryptionService.CreateKeyVaultJwe(cert)
        };
    }

    public CertificateBundle RestoreCertificate(ValueModel<string> backup)
    {
        ArgumentNullException.ThrowIfNull(backup);

        return encryptionService.DecryptFromKeyVaultJwe<CertificateBundle>(backup.Value);
    }

    public ListResult<CertificateVersionItem> GetCertificateVersions(string name, int maxResults = 25, int skipCount = 25)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (maxResults is default(int) && skipCount is default(int))
            return new();

        var allItems = _certs.Where(x => x.Key.Contains(name)).ToList();

        if (allItems.Count == 0)
            return new();

        var maxedItems = allItems.Skip(skipCount).Take(maxResults).Select(x => x.Value);

        var requiresPaging = maxedItems.Count() >= maxResults;

        return new ListResult<CertificateVersionItem>
        {
            NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
            Values = maxedItems.Select(ToCertificateVersionItem)
        };
    }

    public ListResult<CertificateVersionItem> GetCertificates(int maxResults = 25, int skipCount = 25)
    {
        if (maxResults is default(int) && skipCount is default(int))
            return new();

        var allItems = _certs.ToList();

        if (allItems.Count == 0)
            return new();

        var maxedItems = allItems.Skip(skipCount).Take(maxResults).Select(x => x.Value);

        var requiresPaging = maxedItems.Count() >= maxResults;

        return new ListResult<CertificateVersionItem>
        {
            NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
            Values = maxedItems.Select(ToCertificateVersionItem)
        };
    }

    public CertificateBundle ImportCertificate(string name, ImportCertificateRequest request)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var version = Guid.NewGuid().Neat();

        var certificate = X509CertificateFactory.ImportFromBase64(request.Value);

        var (backingKey, backingSecret) = backingService.GetBackingComponents(name);

        var attributes = new CertificateAttributesModel
        {
            Version = version,
            NotBefore = certificate.NotBefore.ToUnixTimeSeconds(),
            Expiration = certificate.NotAfter.ToUnixTimeSeconds()
        };

        var certIdentifier = httpContextAccessor.BuildIdentifierUri(name, version, "certificates");

        var bundle = new CertificateBundle
        {
            CertificateIdentifier = certIdentifier,
            Attributes = attributes,
            CertificateName = name,
            VaultUri = new Uri(AuthConstants.EmulatorUri),
            CertificatePolicy = UpdateNullablePolicy(request.Policy, certIdentifier, attributes),
            X509Thumbprint = certificate.Thumbprint,
            CertificateContents = Convert.ToBase64String(certificate.RawData),
            SecretId = backingSecret.SecretIdentifier.ToString(),
            KeyId = backingKey.Key.KeyIdentifier,

            FullCertificate = certificate
        };

        _certs.SafeAddOrUpdate(name.GetCacheId(), bundle);
        _certs.SafeAddOrUpdate(name.GetCacheId(version), bundle);

        return bundle;
    }

    public CertificateBundle MergeCertificates(string name, MergeCertificatesRequest request)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = _certs.SafeGet(name.GetCacheId());

        ArgumentNullException.ThrowIfNull(cert.FullCertificate);

        var mergedCert = X509CertificateFactory.MergeCertificates(cert.FullCertificate, request.Certificates);

        var version = Guid.NewGuid().Neat();

        var copied = cert.CopyWithNewCertificate(mergedCert);

        _certs.SafeAddOrUpdate(name.GetCacheId(version), copied);

        return copied;
    }

    public CertificateOperation DeleteCertificate(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = _certs.SafeGet(name.GetCacheId());

        var matches = _certs.Where(x => x.Key.Contains(name));

        foreach (var deleted in matches)
            _certs.SafeRemove(deleted.Key);

        var deletedCert = new DeletedCertificateBundle
        {
            Name = name,
            CertificateIdentifier = $"{AuthConstants.EmulatorUri}/certificates/{name}",
            RecoveryId = cert.CertificateIdentifier,
            ContentType = cert.CertificatePolicy?.SecretProperies?.ContentType ?? string.Empty,
            Attributes = cert.Attributes,
            Tags = cert.Tags,
            Kid = cert.KeyId,
            SecretId = cert.SecretId,
            Policy = cert.CertificatePolicy ?? new(),
            CertificateThumbprint = cert.X509Thumbprint,
            CertBase64 = cert.CertificateContents,

            FullCertificate = cert
        };

        _deletedCerts.SafeAddOrUpdate(name.GetCacheId(), deletedCert);

        return new(deletedCert.RecoveryId, name.GetCacheId());
    }

    public CertificateOperation GetPendingDeletedCertificate(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = _deletedCerts.SafeGet(name.GetCacheId());

        return new(cert.RecoveryId, name.GetCacheId());
    }

    public ListResult<DeletedCertificateBundle> GetDeletedCertificates(int maxResults = 25, int skipCount = 25)
    {
        if (maxResults is default(int) && skipCount is default(int))
            return new();

        var allItems = _deletedCerts.ToList();

        if (allItems.Count == 0)
            return new();

        var maxedItems = allItems.Skip(skipCount).Take(maxResults);

        var requiresPaging = maxedItems.Count() >= maxResults;

        return new ListResult<DeletedCertificateBundle>
        {
            NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
            Values = maxedItems.Select(x => x.Value)
        };
    }

    public CertificateOperation GetDeletedCertificate(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = _deletedCerts.SafeGet(name.GetCacheId());

        return new CertificateOperation(cert.RecoveryId, name.GetCacheId());
    }

    public CertificateOperation RecoverCerticate(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cacheId = name.GetCacheId();

        var deletedCert = _deletedCerts.SafeGet(cacheId);

        var cert = deletedCert.FullCertificate;

        ArgumentNullException.ThrowIfNull(cert);

        _certs.SafeAddOrUpdate(cacheId, cert);
        _deletedCerts.SafeRemove(cacheId);

        return new(cert.CertificateIdentifier, name);
    }

    public CertificateOperation GetPendingRecoveryOperation(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = _certs.SafeGet(name.GetCacheId());

        return new(cert.CertificateIdentifier, name);
    }

    public void PurgeDeletedCertificate(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        _deletedCerts.SafeRemove(name);
    }

    private string GenerateNextLink(int maxResults)
    {
        var skipToken = tokenService.CreateSkipToken(maxResults);

        return httpContextAccessor.GetNextLink(skipToken, maxResults);
    }

    private static CertificateVersionItem ToCertificateVersionItem(CertificateBundle bundle)
    {
        return new()
        {
            Id = bundle.CertificateIdentifier,
            Attributes = bundle.Attributes,
            Thumbprint = bundle.X509Thumbprint,
            Tags = bundle.Tags,
        };
    }

    private static CertificatePolicy UpdateNullablePolicy(
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
