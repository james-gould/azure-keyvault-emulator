using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Certificates.Services;

public interface ICertificateService
{
    Task<CertificateOperation> CreateCertificateAsync(string name, CertificateAttributesModel attributes, CertificatePolicy? policy, Dictionary<string, string>? tags = null);
    Task<CertificateBundle> GetCertificateAsync(string name, string version = "");
    Task<ListResult<CertificateVersionItem>> GetCertificatesAsync(int maxResults = 25, int skipToken = 25);
    Task<ListResult<CertificateVersionItem>> GetCertificateVersionsAsync(string name, int maxResults = 25, int skipCount = 25);

    Task<CertificateOperation> GetPendingCertificateAsync(string name);
    Task<CertificatePolicy> UpdateCertificatePolicyAsync(string name, CertificatePolicy certificatePolicy);
    Task<CertificatePolicy> GetCertificatePolicyAsync(string name);
    Task<IssuerBundle> GetCertificateIssuerAsync(string name);

    Task<ValueModel<string>> BackupCertificateAsync(string name);
    Task<CertificateBundle> RestoreCertificateAsync(ValueModel<string> backup);
    Task<CertificateBundle> ImportCertificateAsync(string name, ImportCertificateRequest request);
    Task<CertificateBundle> MergeCertificatesAsync(string name, MergeCertificatesRequest request);

    Task<CertificateOperation> GetDeletedCertificateAsync(string name);
    Task<CertificateOperation> GetPendingDeletedCertificateAsync(string name);

    Task<CertificateOperation> DeleteCertificateAsync(string name);
    Task<ListResult<CertificateBundle>> GetDeletedCertificatesAsync(int maxResults = 25, int skipCount = 25);

    Task<CertificateOperation> GetPendingRecoveryOperationAsync(string name);
    Task<CertificateOperation> RecoverCerticateAsync(string name);
    Task PurgeDeletedCertificateAsync(string name);
}
