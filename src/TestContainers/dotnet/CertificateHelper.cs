using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Net;

namespace AzureKeyVaultEmulator.TestContainers;

/// <summary>
/// Helper class for generating SSL certificates for the Azure KeyVault Emulator.
/// </summary>
internal static class CertificateHelper
{
    private const string _certificatePassword = "emulator";
    private const string _subject = "CN=localhost";

    /// <summary>
    /// Generates SSL certificates for the Azure KeyVault Emulator if they don't exist.
    /// </summary>
    /// <param name="certificatesDirectory">The directory to store certificates in.</param>
    /// <returns>True if certificates were generated or already exist, false otherwise.</returns>
    internal static bool EnsureCertificatesExist(string certificatesDirectory)
    {
        try
        {
            if (!Directory.Exists(certificatesDirectory))
            {
                Directory.CreateDirectory(certificatesDirectory);
            }

            var pfxPath = Path.Combine(certificatesDirectory, AzureKeyVaultEmulatorConstants.RequiredPfxFileName);
            var crtPath = Path.Combine(certificatesDirectory, AzureKeyVaultEmulatorConstants.CrtFileName);

            // Check if certificates already exist
            if (File.Exists(pfxPath) && File.Exists(crtPath))
            {
                return true;
            }

            // Generate new certificates
            var (pfx, pem) = GenerateCertificates();

            // Save certificates to disk
            var pfxBytes = pfx.Export(X509ContentType.Pfx, _certificatePassword);
            File.WriteAllBytes(pfxPath, pfxBytes);
            File.WriteAllText(crtPath, pem);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a self-signed certificate for localhost.
    /// </summary>
    /// <returns>A tuple containing the certificate and PEM data.</returns>
    private static (X509Certificate2 cert, string pem) GenerateCertificates()
    {
        var subject = new X500DistinguishedName(_subject);
        using var rsa = RSA.Create();

        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var san = BuildSubjectAlternativeNames();

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        request.CertificateExtensions.Add(san);

        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        // Setting FriendlyName is only supported on Windows
        if (OperatingSystem.IsWindows())
        {
            cert.FriendlyName = "Azure Key Vault Emulator";
        }

        var pem = ExportToPem(cert);

        return (cert, pem);
    }

    /// <summary>
    /// Builds the Subject Alternative Names extension for localhost.
    /// </summary>
    /// <returns>The SAN extension.</returns>
    private static X509Extension BuildSubjectAlternativeNames()
    {
        var builder = new SubjectAlternativeNameBuilder();

        builder.AddDnsName("localhost");
        builder.AddIpAddress(IPAddress.Parse("127.0.0.1"));

        return builder.Build();
    }

    /// <summary>
    /// Exports a certificate to PEM format.
    /// </summary>
    /// <param name="cert">The certificate to export.</param>
    /// <returns>The PEM-formatted certificate.</returns>
    private static string ExportToPem(X509Certificate2 cert)
    {
        var builder = new StringBuilder();

        builder.AppendLine("-----BEGIN CERTIFICATE-----");
        builder.AppendLine(Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine("-----END CERTIFICATE-----");

        return builder.ToString();
    }
}