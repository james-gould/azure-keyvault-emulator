using AzureKeyVaultEmulator.Shared.Models.Certificates;

namespace AzureKeyVaultEmulator.Certificates.Services;

public sealed class CertificateService : ICertificateService
{
    private static readonly ConcurrentDictionary<string, CertificateBundle> _certs = [];

    public CertificateBundle GetCertificate(string name, string version)
    {
        return _certs.SafeGet(name.GetCacheId(version));
    }
}
