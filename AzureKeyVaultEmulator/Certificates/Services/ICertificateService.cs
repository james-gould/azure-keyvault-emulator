using AzureKeyVaultEmulator.Shared.Models.Certificates;

namespace AzureKeyVaultEmulator.Certificates.Services;

public interface ICertificateService
{
    CertificateBundle GetCertificate(string name, string version);
    CertificateBundle CreateCertificate(string name, CertificateAttributesModel attributes, CertificatePolicy? policy);
}
