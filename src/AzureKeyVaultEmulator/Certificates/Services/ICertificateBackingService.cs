using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Certificates.Services;

public interface ICertificateBackingService
{
    (KeyBundle backingKey, SecretBundle backingSecret) GetBackingComponents(string certName, CertificatePolicy? policy = null);

    IssuerBundle GetIssuer(string name);
    IssuerBundle PersistIssuerConfig(string name, IssuerBundle bundle);
    IssuerBundle AllocateIssuerToCertificate(string certName, IssuerBundle bundle);

    IssuerBundle UpdateCertificateIssuer(string issuerName, IssuerBundle bundle);
    IssuerBundle DeleteIssuer(string issuerName);

    CertificateContacts SetContactInformation(SetContactsRequest request);
    CertificateContacts DeleteCertificateContacts();
    CertificateContacts GetCertificateContacts();
}
