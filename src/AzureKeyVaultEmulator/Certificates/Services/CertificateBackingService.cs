using System.Security.Cryptography.X509Certificates;
using AzureKeyVaultEmulator.Certificates.Factories;
using AzureKeyVaultEmulator.Keys.Services;
using AzureKeyVaultEmulator.Secrets.Services;
using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Models.Secrets.Requests;
using AzureKeyVaultEmulator.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AzureKeyVaultEmulator.Certificates.Services;

public sealed class CertificateBackingService(
    IKeyService keyService,
    ISecretService secretService,
    VaultContext context)
    : ICertificateBackingService
{
    public async Task<IssuerBundle> GetIssuerAsync(string name)
    {
        return await context.Issuers.SafeGetAsync<IssuerBundle, IssuerAttributes>(name);
    }

    public async Task<(KeyBundle backingKey, SecretBundle backingSecret)> GetBackingComponentsAsync(
        string certName,
        X509Certificate2? certificate,
        string? certificatePassword = null,
        CertificatePolicy? policy = null,
        X509ContentType contentType = X509ContentType.Pfx)
    {
        ArgumentException.ThrowIfNullOrEmpty(certName);
        ArgumentNullException.ThrowIfNull(certificate);

        var keySize = policy?.KeyProperties?.KeySize ?? 2048;
        var keyType = !string.IsNullOrEmpty(policy?.KeyProperties?.JsonWebKeyType) ? policy.KeyProperties.JsonWebKeyType : SupportedKeyTypes.RSA;

        var backingKey = await CreateBackingKeyAsync(certName, keySize, keyType);

        var backingSecret = await CreateBackingSecretAsync(certName, contentType, certificate, null);

        return (backingKey, backingSecret);
    }

    public async Task<IssuerBundle> DeleteIssuerAsync(string issuerName)
    {
        ArgumentException.ThrowIfNullOrEmpty(issuerName);

        var bundle = await context.Issuers.SafeGetAsync<IssuerBundle, IssuerAttributes>(issuerName);

        bundle.Deleted = true;

        await context.SaveChangesAsync();

        return bundle;
    }

    public async Task<IssuerBundle> CreateIssuerAsync(string issuerName, IssuerBundle bundle)
    {
        ArgumentException.ThrowIfNullOrEmpty(issuerName);
        ArgumentNullException.ThrowIfNull(bundle);

        var version = Guid.NewGuid().Neat();

        bundle.IssuerName = issuerName;
        bundle.PersistedName = issuerName;
        bundle.PersistedVersion = version;

        await context.Issuers.SafeAddAsync(issuerName, version, bundle);

        await context.SaveChangesAsync();

        return bundle;
    }

    public async Task<IssuerBundle> AllocateIssuerToCertificateAsync(string certName, IssuerBundle bundle)
    {
        ArgumentException.ThrowIfNullOrEmpty(certName);
        ArgumentNullException.ThrowIfNull(bundle);

        var cert = await context.Certificates.SafeGetAsync<CertificateBundle, CertificateAttributes>(certName);
        var issuer = await context.Issuers.SafeGetAsync<IssuerBundle, IssuerAttributes>(bundle.IssuerName);

        if (cert.CertificatePolicy == null)
            throw new MissingItemException($"Certificate {certName} does not have an associated policy to update");

        if (issuer == null)
            throw new InvalidOperationException($"Existing issuer for name {bundle.IssuerName} does not exist.");

        cert.CertificatePolicy.IssuerId = issuer.PersistedId;

        await context.SaveChangesAsync();

        return bundle;
    }

    public async Task<IssuerBundle> UpdateCertificateIssuerAsync(string issuerName, IssuerBundle bundle)
    {
        ArgumentException.ThrowIfNullOrEmpty(issuerName);
        ArgumentNullException.ThrowIfNull(bundle);

        var fromStore = await context.Issuers.SafeGetAsync<IssuerBundle, IssuerAttributes>(issuerName);

        fromStore.OrganisationDetails = bundle.OrganisationDetails;
        fromStore.Attributes = bundle.Attributes;
        fromStore.Credentials = bundle.Credentials;
        fromStore.Provider = bundle.Provider;

        await context.SaveChangesAsync();

        return bundle;
    }

    public async Task<CertificateContacts> SetContactInformationAsync(SetContactsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = Guid.NewGuid().Neat();
        var version = Guid.NewGuid().Neat();

        var existing = await context.CertificateContacts.Where(x => x.Deleted == false).FirstOrDefaultAsync();

        // handles start up scripts, persisted data test runs etc.
        if(existing != null)
        {
            if (existing.Contacts == request.Contacts)
                return existing;
        }

        var contacts = new CertificateContacts
        {
            PersistedName = name,
            PersistedVersion = version,
            Contacts = request.Contacts,
        };

        await context.CertificateContacts.SafeAddAsync(name, version, contacts);

        await context.SaveChangesAsync();

        return contacts;
    }

    public async Task<CertificateContacts> DeleteCertificateContactsAsync()
    {
        var contacts = await context.CertificateContacts.Where(x => x.Deleted == false).FirstOrDefaultAsync();

        if (contacts is null)
            return new();

        contacts.Deleted = true;

        await context.SaveChangesAsync();

        return contacts;
    }

    public async Task<CertificateContacts> GetCertificateContactsAsync()
    {
        var contacts = await context.CertificateContacts.Where(x => x.Deleted == false).FirstOrDefaultAsync();

        return contacts ??= new();
    }

    private async Task<KeyBundle> CreateBackingKeyAsync(
        string certName,
        int keySize,
        string keyType)
    {
        return await keyService.CreateKeyAsync(certName, new CreateKey { KeySize = keySize, KeyType = keyType });
    }

    private async Task<SecretBundle> CreateBackingSecretAsync(
        string certName,
        X509ContentType contentType,
        X509Certificate2 certificate,
        string? certificatePassword = null)
    {
        var certificateData = Convert.ToBase64String(certificate.Export(contentType, certificatePassword));

        return await secretService
            .SetSecretAsync(certName, new SetSecretRequest
            {
                Value = certificateData,
                ContentType = contentType.ParseContentType(),
                SecretAttributes = new()
            });
    }
}
