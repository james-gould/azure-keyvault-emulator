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

    internal static async Task<KeyVaultCertificate> GetCertVersionAsync(
        this CertificateClient client,
        string certName, string version,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetCertificateVersionAsync(certName, version, cancellationToken);
        return response.Value;
    }
    //Idk it is good to use this with "withResponse" or not if good we can add others afterward
    internal static async Task<CertificateIssuer> CreateIssuerWithResponseAsync(
        this CertificateClient client,
        CertificateIssuer certificateIssuer,
        CancellationToken cancellationToken = default)
    {
        var response = await client.CreateIssuerAsync(certificateIssuer, cancellationToken);
        return response.Value;
    }
    internal static async Task<CertificateIssuer> GetIssuerWithResponseAsync(
        this CertificateClient client,
        string? issuerName,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetIssuerAsync(issuerName, cancellationToken);
        return response.Value;
    }
    //i hate naming things..
    internal static async Task<KeyVaultCertificateWithPolicy> RestoreCertificateBackupWithResponseAsync(
       this CertificateClient client,
       byte[] backUp,
       CancellationToken cancellationToken = default)
    {
        var response = await client.RestoreCertificateBackupAsync(backUp, cancellationToken);
        return response.Value;
    }
    internal static async Task<KeyVaultCertificateWithPolicy> ImportCertWithResponseAsync(
      this CertificateClient client,
      ImportCertificateOptions? opt,
      CancellationToken cancellationToken = default)
    {
        var response = await client.ImportCertificateAsync(opt, cancellationToken);
        return response.Value;
    }
    internal static async Task<KeyVaultCertificateWithPolicy> MergeCertWithResponseAsync(
      this CertificateClient client,
      MergeCertificateOptions? opt,
      CancellationToken cancellationToken = default)
    {
        var response = await client.MergeCertificateAsync(opt, cancellationToken);
        return response.Value;
    }
    internal static async Task<KeyVaultCertificateWithPolicy> MergeDeletedCertWithResponseAsync(
      this CertificateClient client,
      string? certName,
      CancellationToken cancellationToken = default)
    {
        var response = await client.GetDeletedCertificateAsync(certName, cancellationToken);
        return response.Value;
    }
    internal static async Task<byte[]> BackupCertificateWithResponseAsync(
    this CertificateClient client,
    string? certName,
    CancellationToken cancellationToken = default)
    {
        var response = await client.BackupCertificateAsync(certName, cancellationToken);
        return response.Value;
    }
    internal static async Task<CertificatePolicy> UpdateCertificatePolicyWithResponseAsync(
    this CertificateClient client,
    string? certName,
    CertificatePolicy policy,
    CancellationToken cancellationToken = default)
    {
        var response = await client.UpdateCertificatePolicyAsync(certName, policy, cancellationToken);
        return response.Value;
    }
    //well after this point I think I might make this more generic ha ?
    //internal static async Task<T> ExecuteWithValueAsync<T,T1>(this CertificateClient client,
    //    Func<CertificateClient, T1, CancellationToken, Task<Azure.Response<T>>> func,
    //    T1 arg,
    //    CancellationToken cancellationToken = default)
    //{
    //    var response = await func(client,arg,cancellationToken);
    //    return response.Value;
    //}
    //not using this because it seems overengineering, but open for any suggestion.
    // Insert other client extensions below and refactor

}
