using AzureKeyVaultEmulator.Shared.Models.Certificates;

namespace AzureKeyVaultEmulator.Certificates.Services;

public interface ICertificateService
{
    CertificateBundle GetCertificate(string name);

    CertificateOperation CreateCertificate(string name, CertificateAttributesModel attributes, CertificatePolicy? policy);
    CertificateOperation GetPendingCertificate(string name);
    CertificatePolicy UpdateCertificatePolicy(string name, CertificatePolicy certificatePolicy);
}
