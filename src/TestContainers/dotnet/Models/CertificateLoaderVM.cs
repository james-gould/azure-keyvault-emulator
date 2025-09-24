using System.Security.Cryptography.X509Certificates;

namespace AzureKeyVaultEmulator.TestContainers.Models
{
    internal sealed class CertificateLoaderVM
    {
        public CertificateLoaderVM(string path)
        {
            LocalCertificatePath = path;
        }

        public string LocalCertificatePath { get; }
        public X509Certificate2? Pfx { get; set; }
        public string pem { get; set; } = string.Empty;
    }
}
