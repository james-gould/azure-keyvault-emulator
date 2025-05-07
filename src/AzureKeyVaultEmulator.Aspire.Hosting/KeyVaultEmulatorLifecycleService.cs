using System.Diagnostics;
using AzureKeyVaultEmulator.Aspire.Hosting.Constants;
using Microsoft.Extensions.Hosting;

namespace AzureKeyVaultEmulator.Aspire.Hosting;

internal sealed class KeyVaultEmulatorLifecycleService(string certificatePath, IHostApplicationLifetime? lifetime) : IHostedService, IAsyncDisposable
{
    private EmulatorCertificates? _certs;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var pfx = Path.Combine(certificatePath, KeyVaultEmulatorCertConstants.Pfx);
        var pem = Path.Combine(certificatePath, KeyVaultEmulatorCertConstants.Crt);

        _certs = new(pfx, pem);

        lifetime?.ApplicationStopping.Register(ExecuteCertificateCleanup);

        return Task.CompletedTask;
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
