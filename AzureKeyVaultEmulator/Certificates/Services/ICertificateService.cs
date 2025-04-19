using AzureKeyVaultEmulator.Shared.Models.Certificates;

namespace AzureKeyVaultEmulator.Certificates.Services;

public interface ICertificateService
{
    CertificateBundle GetCertificate(string name);

    CertificateOperation CreateCertificate(string name, CertificateAttributesModel attributes, CertificatePolicy? policy);
    CertificateOperation GetPendingCertificate(string name);
    CertificateBundle UpdateCertificate(string name, string version, CertificateAttributesModel? attributesModel, CertificatePolicy? policy, Dictionary<string, string>? tags);
}
