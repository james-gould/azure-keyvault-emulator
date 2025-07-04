using System.Diagnostics;
using AzureKeyVaultEmulator.Aspire.Hosting.Constants;
using AzureKeyVaultEmulator.Aspire.Hosting.Exceptions;
using Microsoft.Extensions.Hosting;

namespace AzureKeyVaultEmulator.Aspire.Hosting;

internal sealed class KeyVaultEmulatorLifecycleHelper(
    Func<string> getEndpoint,
    bool forceCleanup,
    string certificatePath,
    IHostApplicationLifetime? lifetime) : IHostedService, IAsyncDisposable
{
    private EmulatorCertificates? _certs;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (forceCleanup)
            RegisterCleanup();

        await EnsureContainerStartAsync();
    }

    /// <summary>
    /// <para>Ungodly bodge to force the AppHost to wait for the Emulator to launch and return a HTTP 200 from {baseUrl}/.</para>
    /// <para>Unable to find a way to add a custom health check /within/ the extensions that doesn't require external use.</para>
    /// <para>Hooking into the ResourceReadyEvent seems to change the behaviour and presents a race condition on launch.</para>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="KeyVaultEmulatorException"></exception>
    private async Task EnsureContainerStartAsync()
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri(getEndpoint());

        for (var i = 1; i <= 5; i++)
        {
            using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

            try
            {
                var response = await client.GetAsync("/token", source.Token);

                if (response.IsSuccessStatusCode)
                    return;
            }
            catch { }

            await Task.Delay(i * 500);
        }

        throw new KeyVaultEmulatorException("Failed to ensure healthy Key Vault Emulator container start.");
    }

    private void RegisterCleanup()
    {
        var pfx = Path.Combine(certificatePath, KeyVaultEmulatorCertConstants.Pfx);
        var pem = Path.Combine(certificatePath, KeyVaultEmulatorCertConstants.Crt);

        _certs = new(pfx, pem);

        lifetime?.ApplicationStopping.Register(ExecuteCertificateCleanup);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            ExecuteCertificateCleanup();
        }
        catch (Exception)
        {
            Debug.WriteLine($"Failed to clean up Emulator certificates at provided path: {certificatePath}");
        }

        return Task.CompletedTask;
    }

    private void ExecuteCertificateCleanup()
    {
        if (string.IsNullOrWhiteSpace(certificatePath))
            return;

        if (!Path.Exists(certificatePath))
            return;

        if (_certs is null)
            return;

        if(Path.Exists(_certs.PFX))
            File.Delete(_certs.PFX);

        if(Path.Exists(_certs.CRT))
            File.Delete(_certs.CRT);
    }

    public ValueTask DisposeAsync()
    {
        if (_certs is null)
            return default;

        if(Path.Exists(_certs.PFX))
            throw new InvalidOperationException($"Failed to clean up certificate {_certs.PFX}, please manually remove it.");

        if(Path.Exists(_certs.CRT))
            throw new InvalidOperationException($"Failed to clean up certificate {_certs.PFX}, please manually remove it.");

        return default;
    }

    private record EmulatorCertificates(string PFX, string CRT);
}
