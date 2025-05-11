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
        return await context.Issuers.SafeGetAsync(name);
    }

    public async Task<(KeyBundle backingKey, SecretBundle backingSecret)> GetBackingComponentsAsync(string certName, CertificatePolicy? policy = null)
    {
        var keySize = policy?.KeyProperties?.KeySize ?? 2048;
        var keyType = !string.IsNullOrEmpty(policy?.KeyProperties?.JsonWebKeyType) ? policy.KeyProperties.JsonWebKeyType : SupportedKeyTypes.RSA;

        var backingKey = await CreateBackingKeyAsync(certName, keySize, keyType);

        var contentType = policy?.SecretProperies?.ContentType ?? "application/unknown";
        var backingSecret = await CreateBackingSecretAsync(certName, contentType);

        return (backingKey, backingSecret);
    }

    public async Task<IssuerBundle> DeleteIssuerAsync(string issuerName)
    {
        ArgumentException.ThrowIfNullOrEmpty(issuerName);

        var bundle = await context.Issuers.SafeGetAsync(issuerName);

        bundle.Deleted = true;

        await context.SaveChangesAsync();

        return bundle;
    }

    public async Task<IssuerBundle> PersistIssuerConfigAsync(string issuerName, IssuerBundle bundle)
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

        var cert = await context.Certificates.SafeGetAsync(certName);

        if (cert.CertificatePolicy == null)
            throw new MissingItemException($"Certificate {certName} does not have an associated policy to update");

        cert.CertificatePolicy.Issuer = bundle;

        await context.SaveChangesAsync();

        return bundle;
    }

    public async Task<IssuerBundle> UpdateCertificateIssuerAsync(string issuerName, IssuerBundle bundle)
    {
        ArgumentException.ThrowIfNullOrEmpty(issuerName);
        ArgumentNullException.ThrowIfNull(bundle);

        var fromStore = await context.Issuers.SafeGetAsync(issuerName);

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
        var contacts = await context.CertificateContacts.Where(x => x.Deleted == false).SingleAsync();

        contacts.Deleted = true;

        await context.SaveChangesAsync();

        return contacts;
    }

    public async Task<CertificateContacts> GetCertificateContactsAsync()
    {
        return await context.CertificateContacts.SingleAsync();
    }

    private async Task<KeyBundle> CreateBackingKeyAsync(
        string certName,
        int keySize,
        string keyType)
    {
        return await keyService.CreateKeyAsync(certName, new CreateKeyModel { KeySize = keySize, KeyType = keyType });
    }

    private async Task<SecretBundle> CreateBackingSecretAsync(string certName, string contentType)
    {
        return await secretService
            .SetSecretAsync(certName, new SetSecretRequest
            {
                Value = Guid.NewGuid().Neat(),
                SecretAttributes = new() { ContentType = contentType }
            });
    }
}
