using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using AzureKeyVaultEmulator.Aspire.Hosting.Constants;
using AzureKeyVaultEmulator.Aspire.Hosting.Exceptions;

namespace AzureKeyVaultEmulator.Aspire.Hosting;

internal static class KeyVaultEmulatorCertHelper
{
    /// <summary>
    /// <para>Gets the path where the certificates are stored on the host machine.</para>
    /// <para>This is then used with the -v arg in Docker to mount the directory as a volume.</para>
    /// </summary>
    /// <returns></returns>
    internal static string GetConfigurableCertStoragePath(string? baseDir = null)
    {
        if (!string.IsNullOrEmpty(baseDir))
            return baseDir;

        if (OperatingSystem.IsWindows())
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        else if (OperatingSystem.IsMacOS())
            baseDir = KeyVaultEmulatorCertConstants.OSXPath;

        else
            baseDir = KeyVaultEmulatorCertConstants.LinuxPath;

        return Path.Combine(
            baseDir,
            KeyVaultEmulatorCertConstants.HostParentDirectory,
            "certs"
        );
    }

    /// <summary>
    /// <para>Generates, trusts and stores a self signed certificate for the subject "localhost".</para>
    /// </summary>
    internal static string ValidateOrGenerateCertificate(KeyVaultEmulatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if(!options.ShouldGenerateCertificates)
            return options.LocalCertificatePath;

        var certPath = GetConfigurableCertStoragePath(options.LocalCertificatePath);

        var certDirExists = Directory.Exists(certPath);

        if(!certDirExists)
            Directory.CreateDirectory(certPath);

        var pfxPath = Path.Combine(certPath, KeyVaultEmulatorCertConstants.Pfx);
        var crtPath = Path.Combine(certPath, KeyVaultEmulatorCertConstants.Crt);

        var pfxExists = Path.Exists(pfxPath);
        var crtExists = Path.Exists(crtPath);

        // Both required certs exist so noop.
        // Will also require a cert check for expiration
        // Out of scope for now
        if (pfxExists && crtExists)
            return certPath;

        // One has been deleted, try to remove them both and regenerate
        if((crtExists && !pfxExists) || (pfxExists && !crtExists) && options.ShouldGenerateCertificates)
            TryRemovePreviousCerts(pfxPath, crtPath);

        // Then create files and place at {path}
        var (pfx, cert) = options.ShouldGenerateCertificates
                            ? GenerateAndSaveCert(pfxPath, crtPath)
                            : LoadExistingCertificatesToStore(pfxPath, crtPath);

        TryWriteToStore(pfx, pfxPath, cert);

        return certPath;
    }

    private static (X509Certificate2 pfx, string pem) LoadExistingCertificatesToStore(
        string pfxPath,
        string? pemPath = null)
    {
        ArgumentNullException.ThrowIfNull(pfxPath);

        if (!File.Exists(pfxPath))
            throw new KeyVaultEmulatorException($"PFX not found at path: {pfxPath}");

        var shouldWritePem = string.IsNullOrEmpty(pemPath) && !File.Exists(pemPath);

        try
        {

#if NET9_0_OR_GREATER
            var pfx = X509CertificateLoader.LoadCertificateFromFile(pfxPath);
            var pem = shouldWritePem ? ExportToPem(pfx) : File.ReadAllText(pemPath!);

            if (OperatingSystem.IsLinux() && string.IsNullOrEmpty(pem))
                throw new KeyVaultEmulatorException("PEM/CRT is required for a Linux host machine but was missing.");

            return (pfx, pem);
#elif NET8_0
            var pfx = new X509Certificate2(pfxPath, KeyVaultEmulatorCertConstants.Pword);
            var pem = shouldWritePem ? ExportToPem(pfx) : File.ReadAllText(pemPath!);

            if (OperatingSystem.IsLinux() && string.IsNullOrEmpty(pem))
                throw new KeyVaultEmulatorException("PEM/CRT is required for a Linux host machine but was missing.");

            return (pfx, pem);
#endif
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static (X509Certificate2 pfx, string pem) GenerateAndSaveCert(string pfxPath, string pemPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(pfxPath);
        ArgumentException.ThrowIfNullOrEmpty(pemPath);

        var subject = KeyVaultEmulatorCertConstants.Subject;
        var ecdsa = ECDsa.Create();

        var request = new CertificateRequest(subject, ecdsa, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        var pfxBytes = cert.Export(X509ContentType.Pfx, KeyVaultEmulatorCertConstants.Pword);

        var pem = ExportToPem(cert);

        File.WriteAllBytes(pfxPath, pfxBytes);
        File.WriteAllText(pemPath, pem);

        return (cert, pem);
    }

    private static string ExportToPem(X509Certificate2 cert)
    {
        var builder = new StringBuilder();

        builder.AppendLine("-----BEGIN CERTIFICATE-----");
        builder.AppendLine(Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine("-----END CERTIFICATE-----");

        return builder.ToString();
    }

    private static void TryWriteToStore(X509Certificate2 pfx, string pfxPath, string pem)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                InstallToWindowsTrustStore(pfx);

            else if (OperatingSystem.IsLinux())
                InstallToLinuxShare(pem);

            else if (OperatingSystem.IsMacOS())
                PromptMacUser(pfxPath);
        }
        catch (Exception)
        {
            throw new KeyVaultEmulatorException(@"
                Failed to insert SSL certificates into local Trust Store.
                If this is your first time using the Emulator, or have enabled ForceCleanupOnShutdown, you need to run as Administrator for this process.
            ");
        }
    }

    private static void InstallToWindowsTrustStore(X509Certificate2 cert)
    {
        ArgumentNullException.ThrowIfNull(cert);

        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

        store.Open(OpenFlags.ReadWrite);
        store.Add(cert);
    }

    private static void InstallToLinuxShare(string pem)
    {
        ArgumentException.ThrowIfNullOrEmpty(pem);

        var destination = $"{KeyVaultEmulatorCertConstants.LinuxPath}/{KeyVaultEmulatorCertConstants.Crt}";

        File.WriteAllText(destination, pem);
    }

    private static void PromptMacUser(string pfxPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(pfxPath);

        Debug.WriteLine("To install on macOS trust store, run:");
        Debug.WriteLine($"sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain \"{pfxPath}\"");
    }

    private static void TryRemovePreviousCerts(string pfx, string crt)
    {
        if (File.Exists(pfx))
        {
            File.Delete(pfx);
            Debug.WriteLine($"Found previous {KeyVaultEmulatorCertConstants.HostParentDirectory} PFX and deleted it.");
        }

        if (File.Exists(crt))
        {
            File.Delete(crt);
            Debug.WriteLine($"Found previous {KeyVaultEmulatorCertConstants.HostParentDirectory} PFX and deleted it.");
        }
    }
}
