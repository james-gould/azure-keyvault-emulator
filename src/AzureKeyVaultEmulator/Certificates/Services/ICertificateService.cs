using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Certificates.Services;

public interface ICertificateService
{
    CertificateOperation CreateCertificate(string name, CertificateAttributesModel attributes, CertificatePolicy? policy);
    CertificateBundle GetCertificate(string name, string version = "");
    ListResult<CertificateVersionItem> GetCertificates(int maxResults = 25, int skipToken = 25);
    ListResult<CertificateVersionItem> GetCertificateVersions(string name, int maxResults = 25, int skipCount = 25);
    

    CertificateOperation GetPendingCertificate(string name);
    CertificatePolicy UpdateCertificatePolicy(string name, CertificatePolicy certificatePolicy);
    CertificatePolicy GetCertificatePolicy(string name);
    IssuerBundle GetCertificateIssuer(string name);

    ValueModel<string> BackupCertificate(string name);
    CertificateBundle RestoreCertificate(ValueModel<string> backup);
    CertificateBundle ImportCertificate(string name, ImportCertificateRequest request);
    CertificateBundle MergeCertificates(string name, MergeCertificatesRequest request);

    CertificateOperation GetDeletedCertificate(string name);
    CertificateOperation GetPendingDeletedCertificate(string name);

    CertificateOperation DeleteCertificate(string name);
    ListResult<DeletedCertificateBundle> GetDeletedCertificates(int maxResults = 25, int skipCount = 25);

    CertificateOperation GetPendingRecoveryOperation(string name);
    CertificateOperation RecoverCerticate(string name);
    void PurgeDeletedCertificate(string name);
}
