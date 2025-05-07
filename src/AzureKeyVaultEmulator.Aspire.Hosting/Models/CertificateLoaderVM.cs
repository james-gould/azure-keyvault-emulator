using System.Security.Cryptography.X509Certificates;

namespace AzureKeyVaultEmulator.Aspire.Hosting.Models;

internal sealed class CertificateLoaderVM(string path)
{
    public string LocalCertificatePath => path;
    public X509Certificate2? Pfx { get; set; }
    public string pem { get; set; } = string.Empty;
}
