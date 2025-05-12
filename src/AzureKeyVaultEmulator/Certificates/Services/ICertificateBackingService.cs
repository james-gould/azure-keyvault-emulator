using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Certificates.Services;

public interface ICertificateBackingService
{
    Task<(KeyBundle backingKey, SecretBundle backingSecret)> GetBackingComponentsAsync(string certName, CertificatePolicy? policy = null);

    Task<IssuerBundle> GetIssuerAsync(string name);
    Task<IssuerBundle> CreateIssuerAsync(string name, IssuerBundle bundle);
    Task<IssuerBundle> AllocateIssuerToCertificateAsync(string certName, IssuerBundle bundle);

    Task<IssuerBundle> UpdateCertificateIssuerAsync(string issuerName, IssuerBundle bundle);
    Task<IssuerBundle> DeleteIssuerAsync(string issuerName);

    Task<CertificateContacts> SetContactInformationAsync(SetContactsRequest request);
    Task<CertificateContacts> DeleteCertificateContactsAsync();
    Task<CertificateContacts> GetCertificateContactsAsync();
}
