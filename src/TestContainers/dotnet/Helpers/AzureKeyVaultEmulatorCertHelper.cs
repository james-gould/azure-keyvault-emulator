﻿using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using AzureKeyVaultEmulator.TestContainers.Constants;
using AzureKeyVaultEmulator.TestContainers.Models;
using AzureKeyVaultEmulator.TestContainers.Exceptions;

namespace AzureKeyVaultEmulator.TestContainers.Helpers;

internal static class AzureKeyVaultEmulatorCertHelper
{
    /// <summary>
    /// <para>Generates, trusts and stores a self signed certificate for the subject "localhost".</para>
    /// </summary>
    /// <param name="options">The granular options for configuring the Azure Key Vault Emulator.</param>
    /// <returns>The base directory containing certificates.</returns>
    internal static CertificateLoaderVM ValidateOrGenerateCertificate(AzureKeyVaultEmulatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrEmpty(options.LocalCertificatePath) && !Directory.Exists(options.LocalCertificatePath))
            Directory.CreateDirectory(options.LocalCertificatePath);

        var certPath = GetConfigurableCertStoragePath(options.LocalCertificatePath);

        if (!Directory.Exists(certPath))
            Directory.CreateDirectory(certPath);

        var pfxPath = Path.Combine(certPath, AzureKeyVaultEmulatorCertConstants.Pfx);
        var crtPath = Path.Combine(certPath, AzureKeyVaultEmulatorCertConstants.Crt);

        var certsAlreadyExist = File.Exists(pfxPath) && File.Exists(crtPath);

        // Both required certs exist so noop.
        // Will also require a cert check for expiration
        // Out of scope for now
        if (certsAlreadyExist && !options.LoadCertificatesIntoTrustStore)
            return new(certPath);

        // One has been deleted, try to remove them both and regenerate.
        // Only if users allow us to conduct IO on the certificates for the Emulator.
        if (!certsAlreadyExist && options.ShouldGenerateCertificates)
            TryRemovePreviousCerts(pfxPath, crtPath);

        // Then create files and place at {path}
        var (pfx, pem) = options.ShouldGenerateCertificates && !certsAlreadyExist
                            ? GenerateAndSaveCert(pfxPath, crtPath)
                            : LoadExistingCertificatesToInstall(pfxPath, crtPath);

        return new(certPath) { Pfx = pfx, pem = pem };
    }

    /// <summary>
    /// <para>Gets the path where the certificates are stored on the host machine.</para>
    /// <para>This is then used with the -v arg in Docker to mount the directory as a volume.</para>
    /// </summary>
    /// <returns>The parent directory containing the certificates.</returns>
    internal static string GetConfigurableCertStoragePath(string? baseDir = null)
    {
        // Bypass permission issues when the certificates are throwaway/single use.
        if (AzureKeyVaultEnvHelper.IsCiCdEnvironment())
            return Path.GetTempPath();

        if (!string.IsNullOrEmpty(baseDir))
            return baseDir;

        if (OperatingSystem.IsWindows())
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        else if (OperatingSystem.IsMacOS())
            baseDir = AzureKeyVaultEmulatorCertConstants.OSXPath;

        else
            baseDir = AzureKeyVaultEmulatorCertConstants.LinuxPath;

        return Path.Combine(
            baseDir,
            AzureKeyVaultEmulatorCertConstants.HostParentDirectory,
            AzureKeyVaultEmulatorCertConstants.HostChildDirectory
        );
    }

    /// <summary>
    /// Attempts to find and read the contents of the certificates used for SSL.
    /// </summary>
    /// <param name="pfxPath">The path to the PFX.</param>
    /// <param name="pemPath">The path to the PEM.</param>
    /// <returns>A full <see cref="X509Certificate2"/> and associated PEM from the RawData.</returns>
    /// <exception cref="KeyVaultEmulatorException"></exception>
    private static (X509Certificate2 pfx, string pem) LoadExistingCertificatesToInstall(
        string pfxPath,
        string? pemPath = null)
    {
        ArgumentNullException.ThrowIfNull(pfxPath);

        if (!File.Exists(pfxPath))
            throw new KeyVaultEmulatorException($"PFX not found at path: {pfxPath}");

        var shouldWritePem = string.IsNullOrEmpty(pemPath) || !File.Exists(pemPath);

        try
        {

#if NET9_0_OR_GREATER
            var pfx = X509CertificateLoader.LoadPkcs12FromFile(pfxPath, KeyVaultEmulatorCertConstants.Pword);
            var pem = shouldWritePem ? ExportToPem(pfx) : File.ReadAllText(pemPath!);

            if (OperatingSystem.IsLinux() && string.IsNullOrEmpty(pem))
                throw new KeyVaultEmulatorException("CRT is required for a Linux host machine but was missing at path: {pfxPath}.");

            return (pfx, pem);
#elif NET8_0
            var pfx = new X509Certificate2(pfxPath, AzureKeyVaultEmulatorCertConstants.Pword);
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

    /// <summary>
    /// Creates and writes to disk a PFX and associated PEM, to then be mounted into the Azure Key Vault Emulator.
    /// </summary>
    /// <param name="pfxPath">The path to write the PFX to.</param>
    /// <param name="pemPath">The path to write the PEM to.</param>
    /// <returns>A complete <see cref="X509Certificate2"/> and associated PEM from the RawData.</returns>
    private static (X509Certificate2 pfx, string pem) GenerateAndSaveCert(string pfxPath, string pemPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(pfxPath);
        ArgumentException.ThrowIfNullOrEmpty(pemPath);

        var subject = new X500DistinguishedName(AzureKeyVaultEmulatorCertConstants.Subject);
        using var rsa = RSA.Create();

        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var san = BuildSubjectAlternativeNames();

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        request.CertificateExtensions.Add(san);

        // Sign for digital usage in SSL
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));

        // Add EKU for Server Authentication
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, critical: false));

        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        // Setting FriendlyName is only supported on Windows for some reason.
        if (OperatingSystem.IsWindows())
            cert.FriendlyName = "Azure Key Vault Emulator";

        var pfxBytes = cert.Export(X509ContentType.Pfx, AzureKeyVaultEmulatorCertConstants.Pword);

        var pem = ExportToPem(cert);

        File.WriteAllBytes(pfxPath, pfxBytes);
        File.WriteAllText(pemPath, pem);

        return (cert, pem);
    }

    /// <summary>
    /// Defines the SAN extension used to bind to "localhost"
    /// </summary>
    /// <returns></returns>
    private static X509Extension BuildSubjectAlternativeNames()
    {
        var builder = new SubjectAlternativeNameBuilder();

        builder.AddDnsName("localhost");
        builder.AddIpAddress(IPAddress.Parse("127.0.0.1"));

        return builder.Build();
    }

    /// <summary>
    /// Given a PFX, export the RawData.
    /// </summary>
    /// <param name="cert">The PFX to export from.</param>
    /// <returns>The raw data from the PFX, formatted as a PEM certificate.</returns>
    private static string ExportToPem(X509Certificate2 cert)
    {
        var builder = new StringBuilder();

        builder.AppendLine("-----BEGIN CERTIFICATE-----");
        builder.AppendLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine("-----END CERTIFICATE-----");

        return builder.ToString();
    }

    /// <summary>
    /// Will attempt to install the SSL certificates into the trust store, if permitted.
    /// </summary>
    /// <param name="options">The granular options used to disable attempted writes.</param>
    /// <param name="pfx">The <see cref="X509Certificate2"/> with password to use for secure connections.</param>
    /// <param name="pfxPath">The path to <paramref name="pfx"/></param>
    /// <param name="pem">The raw data or loaded PEM from <paramref name="pfx"/></param>
    /// <exception cref="KeyVaultEmulatorException"></exception>
    internal static void TryWriteToStore(
        AzureKeyVaultEmulatorOptions options,
        X509Certificate2? pfx,
        string pfxPath,
        string pem)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.LoadCertificatesIntoTrustStore)
            return;

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
            throw;
            //throw new KeyVaultEmulatorException($"Failed to insert SSL certificates into local Trust Store. To use the Emulator you must install the certificates at {pfxPath} yourself first, then try again.");
        }
    }

    /// <summary>
    /// Installs the <paramref name="cert"/> to the Windows TrustStore.
    /// </summary>
    /// <param name="cert">The <see cref="X509Certificate2"/> to install.</param>
    private static void InstallToWindowsTrustStore(X509Certificate2? cert)
    {
        if (cert is null)
            return;

        using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);

        store.Open(OpenFlags.ReadWrite);
        store.Add(cert);
    }

    /// <summary>
    /// Installs the PEM to the Linux /usr/local/share/ca-certificates and runs update-ca-certificates
    /// </summary>
    /// <param name="pem">The CRT/PEM to install.</param>
    private static void InstallToLinuxShare(string pem)
    {
        ArgumentException.ThrowIfNullOrEmpty(pem);

        try
        {
            // Need to use /tmp/ during CI/CD to guarantee SSL certs are installed correctly
            if (AzureKeyVaultEnvHelper.IsCiCdEnvironment())
            {
                var tmpCrt = $"/tmp/{AzureKeyVaultEmulatorCertConstants.Crt}";

                File.WriteAllText(tmpCrt, pem);

                AzureKeyVaultEnvHelper.Bash($"sudo cp {tmpCrt} /usr/local/share/ca-certificates/emulator.crt");
            }
            else
            {
                // Otherwise, on a Linux host machine, write directly to usr/local/share/ca-certificates
                var destination = $"{AzureKeyVaultEmulatorCertConstants.LinuxPath}/{AzureKeyVaultEmulatorCertConstants.Crt}";

                File.WriteAllText(destination, pem);
            }
        }
        catch(Exception)
        {
            // Feels weird but only way to give contextual info to user about why it failed...
            throw new InvalidOperationException($"Failed to copy {AzureKeyVaultEmulatorCertConstants.Crt} to {AzureKeyVaultEmulatorCertConstants.LinuxPath}");
        }

        AzureKeyVaultEnvHelper.Bash("sudo update-ca-certificates");
    }

    /// <summary>
    /// <para>Prompts a MacOS user to run a specific command, with <paramref name="pfxPath"/>, to install the certificates.</para>
    /// <para>We are unable to perform this from .NET, the user must do this manually via Terminal.</para>
    /// </summary>
    /// <param name="pfxPath">The path to the PFX to install.</param>
    private static void PromptMacUser(string pfxPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(pfxPath);

        Console.WriteLine("To install on macOS trust store, run:");
        Console.WriteLine($"sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain \"{pfxPath}\"");
    }

    /// <summary>
    /// Attempts to remove lingering certificates if they need to be regenerated.
    /// </summary>
    /// <param name="pfx">Potential PFX to remove.</param>
    /// <param name="pem">Potential PEM to remove.</param>
    private static void TryRemovePreviousCerts(string pfx, string pem)
    {
        if (!string.IsNullOrEmpty(pfx) && File.Exists(pfx))
        {
            File.Delete(pfx);
            Console.WriteLine($"Found previous {AzureKeyVaultEmulatorCertConstants.HostParentDirectory} PFX and deleted it.");
        }

        if (!string.IsNullOrEmpty(pem) && File.Exists(pem))
        {
            File.Delete(pem);
            Console.WriteLine($"Found previous {AzureKeyVaultEmulatorCertConstants.HostParentDirectory} PFX and deleted it.");
        }
    }
}
