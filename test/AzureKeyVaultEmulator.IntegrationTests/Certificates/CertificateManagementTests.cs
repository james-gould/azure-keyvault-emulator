using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

namespace AzureKeyVaultEmulator.IntegrationTests.Certificates;

public class CertificateManagementTests(CertificatesTestingFixture fixture) : IClassFixture<CertificatesTestingFixture>
{
    [Fact]
    public async Task ImportingP12CertificateWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;
        var certPwd = fixture.FreshlyGeneratedGuid;

        var x509 = CreateCertificate(certName);

        var exportedFromRaw = x509.Export(X509ContentType.Pkcs12, certPwd);

        var options = new ImportCertificateOptions(certName, exportedFromRaw)
        {
            Password = certPwd,
        };

        var importedCertificate = await client.ImportCertificateAsync(options);

        Assert.NotNull(importedCertificate.Value);
        Assert.Equal(certName, importedCertificate.Value.Name);
    }

    [Fact]
    public async Task ImportingP12CertificateWillSetCorrectContentType()
    {
        var client = await fixture.GetClientAsync();

        var expectedContentType = "application/x-pkcs12";

        var certName = fixture.FreshlyGeneratedGuid;
        var certPwd = fixture.FreshlyGeneratedGuid;

        var x509 = CreateCertificate(certName);

        var exportedToRaw = x509.Export(X509ContentType.Pkcs12, certPwd);

        var options = new ImportCertificateOptions(certName, exportedToRaw)
        {
            Password = certPwd,
        };

        var importResponse = await client.ImportCertificateAsync(options);

        var importedCertificate = importResponse.Value;

        Assert.NotNull(importedCertificate);
        Assert.Equal(certName, importedCertificate.Name);
        Assert.Equal(expectedContentType, importedCertificate.Policy.ContentType);
    }

    [Fact]
    public async Task ImportingPemCertificateWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;
        var certPwd = fixture.FreshlyGeneratedGuid;

        var x509 = CreateCertificate(certName);

        var pem = x509.ExportCertificatePem();

        var exportedToRaw = Encoding.UTF8.GetBytes(pem);

        var options = new ImportCertificateOptions(certName, exportedToRaw)
        {
            Password = certPwd
        };

        var importResponse = await client.ImportCertificateAsync(options);

        var importedCertificate = importResponse.Value;

        Assert.NotNull(importedCertificate);
        Assert.Equal(certName, importedCertificate.Name);
    }

    [Fact]
    public async Task ImportingPemCertificateWillCorrectContentType()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;
        var certPwd = fixture.FreshlyGeneratedGuid;

        var expectedContentType = "application/x-pem-file";

        var x509 = CreateCertificate(certName);

        var pem = x509.ExportCertificatePem();

        var exportedToRaw = Encoding.UTF8.GetBytes(pem);

        var options = new ImportCertificateOptions(certName, exportedToRaw)
        {
            Password = certPwd
        };

        var importResponse = await client.ImportCertificateAsync(options);

        var importedCertificate = importResponse.Value;

        Assert.NotNull(importedCertificate);
        Assert.Equal(certName, importedCertificate.Name);
        Assert.Equal(expectedContentType, importedCertificate.Policy.ContentType);
    }

    [Fact]
    public async Task ImportedPasswordProtectedCertificateStripsPassword()
    {
        // Arrange: Create and import a password-protected certificate
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;
        var certPwd = fixture.FreshlyGeneratedGuid;

        var x509 = CreateCertificate(certName);

        var exportedFromRaw = x509.Export(X509ContentType.Pkcs12, certPwd);

        var options = new ImportCertificateOptions(certName, exportedFromRaw)
        {
            Password = certPwd,
        };

        await client.ImportCertificateAsync(options);

        // Act: Download the certificate (this retrieves the backing secret)
        var downloadedCertificateResponse = await client.DownloadCertificateAsync(certName);
        var downloadedCertificate = downloadedCertificateResponse.Value;

        // Assert: The downloaded certificate should be usable without password
        // Azure Key Vault strips the password from imported certificates
        Assert.NotNull(downloadedCertificate);
        Assert.True(downloadedCertificate.HasPrivateKey, "Downloaded certificate should have private key");

        // Verify private key is accessible (this would fail if password was retained)
        var privateKey = downloadedCertificate.GetRSAPrivateKey();
        Assert.NotNull(privateKey);
    }

    private static X509Certificate2 CreateCertificate(string name)
    {
        var keySize = 2048;

        using var rsa = RSA.Create(keySize);

        var certName = new X500DistinguishedName($"CN={name}");

        var request = new CertificateRequest(certName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions
            .Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DataEncipherment |
                X509KeyUsageFlags.KeyEncipherment |
                X509KeyUsageFlags.DigitalSignature,
                false)
            );

        return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(365));
    }
}
