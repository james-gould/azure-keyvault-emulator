using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Certificates.Services;

public interface ICertificateBackingService
{
    (KeyBundle backingKey, SecretBundle backingSecret) GetBackingComponents(string certName, CertificatePolicy? policy = null);

    IssuerBundle GetIssuer(string name);
    IssuerBundle PersistIssuerConfig(string name, IssuerBundle bundle);
    IssuerBundle UpdateIssuerAgainstCertificate(string certName, IssuerBundle bundle);

    IssuerBundle DeleteIssuer(string issuerName);
}
