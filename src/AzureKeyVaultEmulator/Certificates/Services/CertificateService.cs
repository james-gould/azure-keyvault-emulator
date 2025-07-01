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
    public async Task<CertificateOperation> CreateCertificateAsync(
        string name,
        CertificateAttributesModel attributes,
        CertificatePolicy? policy,
        Dictionary<string, string>? tags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(attributes);

        var certificate = X509CertificateFactory.BuildX509Certificate(name, policy);

        var (backingKey, backingSecret) = await backingService.GetBackingComponentsAsync(name, certificate, policy);

        var version = Guid.NewGuid().Neat();

        attributes.Version = version;
        attributes.NotBefore = certificate.NotBefore.ToUnixTimeSeconds();
        attributes.Expiration = certificate.NotAfter.ToUnixTimeSeconds();

        var certIdentifier = httpContextAccessor.BuildIdentifierUri(name, version, "certificates");

        var concretePolicy = UpdateNullablePolicy(policy, certIdentifier, attributes);

        var bundle = new CertificateBundle
        {
            PersistedName = name,
            PersistedVersion = version,
            CertificateIdentifier = certIdentifier,
            RecoveryId = certIdentifier,
            Attributes = attributes,
            CertificateName = name,
            VaultUri = new Uri(AuthConstants.EmulatorUri),
            X509Thumbprint = certificate.Thumbprint,
            CertificateContents = Convert.ToBase64String(certificate.RawData),
            CertificatePolicy = concretePolicy,
            SecretId = backingSecret.SecretIdentifier,
            KeyId = backingKey.Key.KeyIdentifier,
            Tags = tags ??= [],

            FullCertificate = certificate
        };

        context.Add(bundle);

        //await context.Certificates.SafeAddAsync(name, version, bundle);

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

        ArgumentNullException.ThrowIfNull(cert.CertificatePolicy);

        policy.IssuerId = cert.CertificatePolicy.IssuerId;
        cert.CertificatePolicy = policy;

        await context.SaveChangesAsync();

        return policy;
    }

    public async Task<CertificateBundle> UpdateCertificateAsync(string name, string? version, UpdateCertificateRequest request)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(request);

        var cert = await context.Certificates.SafeGetAsync(name, version: version ?? string.Empty);

        cert.CertificatePolicy = request.Policy ??= cert.CertificatePolicy;
        cert.Attributes = request.Attributes ?? cert.Attributes;
        cert.Tags = request.Tags ??= cert.Tags;

        await context.SaveChangesAsync();

        return cert;
    }

    public async Task<CertificateOperation> GetPendingCertificateAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = await context.Certificates.SafeGetAsync(name);

        return new CertificateOperation(cert.CertificateIdentifier, name);
    }

    public async Task<IssuerBundle> GetCertificateIssuerAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return await backingService.GetIssuerAsync(name);
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

        policy.IssuerId = bundle.CertificatePolicy.Issuer.PersistedId;

        var newVersion = Guid.NewGuid().Neat();

        await context.Certificates.SafeAddAsync(bundle.PersistedName, newVersion, bundle);
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

        var (backingKey, backingSecret) = await backingService.GetBackingComponentsAsync(name, certificate);

        var attributes = new CertificateAttributesModel
        {
            Version = version,
            NotBefore = certificate.NotBefore.ToUnixTimeSeconds(),
            Expiration = certificate.NotAfter.ToUnixTimeSeconds()
        };

        var certIdentifier = httpContextAccessor.BuildIdentifierUri(name, version, "certificates");

        var bundle = new CertificateBundle
        {
            PersistedName = name,
            PersistedVersion = version,
            CertificateIdentifier = certIdentifier,
            RecoveryId = certIdentifier,
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

    public async Task<CertificateOperation> DeleteCertificateAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = await context.Certificates.SafeGetAsync(name);

        var matches = await context.Certificates.Where(x => x.PersistedName == name).ToListAsync();

        foreach (var match in matches)
            match.Deleted = true;

        cert.Deleted = true;

        await context.SaveChangesAsync();

        return new(cert.RecoveryId, name.GetCacheId());
    }

    public async Task<CertificateOperation> GetPendingDeletedCertificateAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = await context.Certificates.SafeGetAsync(name, deleted: true);

        return new(cert.CertificateIdentifier, name.GetCacheId());
    }

    public async Task<ListResult<CertificateBundle>> GetDeletedCertificatesAsync(int maxResults = 25, int skipCount = 25)
    {
        if (maxResults is default(int) && skipCount is default(int))
            return new();

        var allItems = await context.Certificates.Where(x => x.Deleted == true).ToListAsync();

        if (allItems == null || allItems.Count == 0)
            return new();

        var maxedItems = allItems.Skip(skipCount).Take(maxResults);

        var requiresPaging = maxedItems.Count() >= maxResults;

        return new ListResult<CertificateBundle>
        {
            NextLink = requiresPaging ? GenerateNextLink(maxResults + skipCount) : string.Empty,
            Values = maxedItems
        };
    }

    public async Task<CertificateOperation> GetDeletedCertificateAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = await context.Certificates.SafeGetAsync(name, deleted: true);

        return new CertificateOperation(cert.RecoveryId, name);
    }

    public async Task<CertificateOperation> RecoverCerticateAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var bundle = await context.Certificates.SafeGetAsync(name, deleted: true);

        var fullCertificate = bundle.FullCertificate;

        ArgumentNullException.ThrowIfNull(fullCertificate);

        bundle.Deleted = false;

        await context.SaveChangesAsync();

        return new(bundle.CertificateIdentifier, name);
    }

    public async Task<CertificateOperation> GetPendingRecoveryOperationAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var cert = await context.Certificates.SafeGetAsync(name);

        return new(cert.CertificateIdentifier, name);
    }

    public async Task PurgeDeletedCertificateAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        await context.Certificates.SafeRemoveAsync(name, deleted: true);

        await context.SaveChangesAsync();
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
        var issuer = policy?.Issuer ?? new();
        policy ??= new();

        policy.Identifier = identifier;
        policy.CertificateAttributes ??= attributes;

        policy.Issuer = issuer;
        policy.IssuerId = issuer.PersistedId;

        return policy;
    }
}
