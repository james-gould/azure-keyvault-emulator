using AzureKeyVaultEmulator.Shared.Models.Certificates;

namespace AzureKeyVaultEmulator.Certificates.Services;

public interface ICertificateService
{
    CertificateBundle GetCertificate(string name, string version = "");

    CertificateOperation CreateCertificate(string name, CertificateAttributesModel attributes, CertificatePolicy? policy);
    CertificateOperation GetPendingCertificate(string name);
    CertificatePolicy UpdateCertificatePolicy(string name, CertificatePolicy certificatePolicy);
    CertificatePolicy GetCertificatePolicy(string name);

    IssuerBundle GetCertificateIssuer(string name);
}
