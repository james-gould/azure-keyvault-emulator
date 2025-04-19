using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Certificates.Services;

public interface ICertificateBackingService
{
    (KeyBundle backingKey, SecretBundle backingSecret) GetBackingComponents(string certName, CertificatePolicy? policy = null);
}
