using AzureKeyVaultEmulator.Certificates.Factories;
using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AzureKeyVaultEmulator.Certificates.Services;

public sealed class CertificateService(
    IHttpContextAccessor httpContextAccessor,
    ICertificateBackingService backingService,
    IEncryptionService encryptionService,
    ITokenService tokenService,
    VaultContext context)
    : ICertificateService
{
    private static readonly ConcurrentDictionary<string, DeletedCertificateBundle> _deletedCerts = [];

    public async Task<CertificateOperation> CreateCertificateAsync(
        string name,
        CertificateAttributesModel attributes,
        CertificatePolicy? policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(attributes);

        var certificate = X509CertificateFactory.BuildX509Certificate(name, policy);

        var (backingKey, backingSecret) = await backingService.GetBackingComponents(name, policy);

        var version = Guid.NewGuid().Neat();

        attributes.Version = version;
        attributes.NotBefore = certificate.NotBefore.ToUnixTimeSeconds();
        attributes.Expiration = certificate.NotAfter.ToUnixTimeSeconds();

        var certIdentifier = httpContextAccessor.BuildIdentifierUri(name, version, "certificates");

        var concretePolicy = UpdateNullablePolicy(policy, certIdentifier, attributes);

        var bundle = new CertificateBundle
        {
            CertificateIdentifier = certIdentifier,
            Attributes = attributes,
            CertificateName = name,
            VaultUri = new Uri(AuthConstants.EmulatorUri),
            CertificatePolicy = concretePolicy,
            X509Thumbprint = certificate.Thumbprint,
            CertificateContents = Convert.ToBase64String(certificate.RawData),
            SecretId = backingSecret.SecretIdentifier,
            KeyId = backingKey.Key.KeyIdentifier,

            FullCertificate = certificate
        };

        await context.Certificates.SafeAddAsync(name, version, bundle);
        await context.CertificatePolicies.SafeAddAsync(name, version, concretePolicy);

        await context.SaveChangesAsync();

        return new CertificateOperation(certIdentifier, name);
    }

    public async Task<CertificateBundle> GetCertificateAsync(string name, string version = "")
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return await context.Certificates.SafeGetAsync(name, version);
    }

    public async Task<CertificatePolicy> GetCertificatePolicyAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = await context.Certificates.SafeGetAsync(name);

        return cert.CertificatePolicy
            ?? throw new InvalidOperationException($"Found certificate {cert.CertificateName} but the associated policy was null.");
    }

    public async Task<CertificatePolicy> UpdateCertificatePolicyAsync(string name, CertificatePolicy policy)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = await context.Certificates.SafeGetAsync(name);

        cert.CertificatePolicy = policy;

        await context.SaveChangesAsync();

        backingService.AllocateIssuerToCertificate(name, policy.Issuer);

        return policy;
    }

    public async Task<CertificateOperation> GetPendingCertificateAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = await context.Certificates.SafeGetAsync(name);

        return new CertificateOperation(cert.CertificateIdentifier, name);
    }

    public IssuerBundle GetCertificateIssuer(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return backingService.GetIssuer(name);
    }

    public async Task<ValueModel<string>> BackupCertificateAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = await context.Certificates.SafeGetAsync(name);

        return new ValueModel<string>
        {
            Value = encryptionService.CreateKeyVaultJwe(cert)
        };
    }

    public async Task<CertificateBundle> RestoreCertificateAsync(ValueModel<string> backup)
    {
        ArgumentNullException.ThrowIfNull(backup);

        var bundle = encryptionService.DecryptFromKeyVaultJwe<CertificateBundle>(backup.Value);
        var policy = bundle?.CertificatePolicy ?? throw new InvalidOperationException($"Failed to find {nameof(CertificatePolicy)} in backup.");

        var newVersion = Guid.NewGuid().Neat();

        await context.Certificates.SafeAddAsync(bundle.PersistedName, newVersion, bundle);
        await context.CertificatePolicies.SafeAddAsync(policy.PersistedName, newVersion, policy);

        await context.SaveChangesAsync();

        return bundle;
    }

    public async Task<ListResult<CertificateVersionItem>> GetCertificateVersionsAsync(string name, int maxResults = 25, int skipCount = 25)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (maxResults is default(int) && skipCount is default(int))
            return new();

        var allItems = await context.Certificates.Where(x => x.PersistedName == name).ToListAsync();

        if (allItems.Count == 0)
            return new();

        var maxedItems = allItems.Skip(skipCount).Take(maxResults).Select(x => x);

        var requiresPaging = maxedItems.Count() >= maxResults;

        return new ListResult<CertificateVersionItem>
        {
            NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
            Values = maxedItems.Select(ToCertificateVersionItem)
        };
    }

    public async Task<ListResult<CertificateVersionItem>> GetCertificatesAsync(int maxResults = 25, int skipCount = 25)
    {
        if (maxResults is default(int) && skipCount is default(int))
            return new();

        var allItems = await context.Certificates.ToListAsync();

        if (allItems.Count == 0)
            return new();

        var maxedItems = allItems.Skip(skipCount).Take(maxResults).Select(x => x);

        var requiresPaging = maxedItems.Count() >= maxResults;

        return new ListResult<CertificateVersionItem>
        {
            NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
            Values = maxedItems.Select(ToCertificateVersionItem)
        };
    }

    public async Task<CertificateBundle> ImportCertificateAsync(string name, ImportCertificateRequest request)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var version = Guid.NewGuid().Neat();

        var certificate = X509CertificateFactory.ImportFromBase64(request.Value);

        var (backingKey, backingSecret) = await backingService.GetBackingComponents(name);

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

        await context.Certificates.SafeAddAsync(name, version, bundle);

        await context.SaveChangesAsync();

        return bundle;
    }

    public async Task<CertificateBundle> MergeCertificatesAsync(string name, MergeCertificatesRequest request)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = await context.Certificates.SafeGetAsync(name.GetCacheId());

        ArgumentNullException.ThrowIfNull(cert.FullCertificate);

        var mergedCert = X509CertificateFactory.MergeCertificates(cert.FullCertificate, request.Certificates);

        var version = Guid.NewGuid().Neat();

        var copied = cert.CopyWithNewCertificate(mergedCert);

        await context.Certificates.SafeAddAsync(name, version, copied);

        await context.SaveChangesAsync();

        return copied;
    }

    public async Task<CertificateOperation> DeleteCertificateAsync(string name) //TODO
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = await context.Certificates.SafeGetAsync(name);

        var matches = await context.Certificates.Where(x => x.PersistedName == name).ToListAsync();

        //foreach (var deleted in matches)
        //    _certs.SafeRemove(deleted.Key);

        var deletedCert = new DeletedCertificateBundle
        {
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

    public async Task<CertificateOperation> RecoverCerticateAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var deletedCert = _deletedCerts.SafeGet(name);

        var cert = deletedCert.FullCertificate;

        ArgumentNullException.ThrowIfNull(cert);

        await context.Certificates.SafeAddAsync(name, "", cert);

        await context.SaveChangesAsync();

        _deletedCerts.SafeRemove(name);

        return new(cert.CertificateIdentifier, name);
    }

    public async Task<CertificateOperation> GetPendingRecoveryOperationAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = await context.Certificates.SafeGetAsync(name);

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
