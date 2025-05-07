using Azure.Security.KeyVault.Certificates;

namespace AzureKeyVaultEmulator.IntegrationTests.Extensions;

internal static class AzureClientExtensions
{
    /// <summary>
    /// Unwraps the <see cref="Azure.Response"/> structure from the client to keep tests a bit cleaner.
    /// </summary>
    internal static async Task<KeyVaultCertificateWithPolicy> GetCertAsync(
        this CertificateClient client,
        string certName,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetCertificateAsync(certName, cancellationToken);

        return response.Value;
    }

    // Insert other client extensions below and refactor
}
