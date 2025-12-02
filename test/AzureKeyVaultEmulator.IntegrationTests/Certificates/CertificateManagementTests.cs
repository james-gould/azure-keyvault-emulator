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

    [Fact]
    public async Task ImportedCertificateCreatesBackingSecret()
    {
        // Arrange: Import a certificate
        var certClient = await fixture.GetClientAsync();
        var secretClient = await fixture.GetSecretClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;
        var certPwd = fixture.FreshlyGeneratedGuid;

        var x509 = CreateCertificate(certName);

        var exportedFromRaw = x509.Export(X509ContentType.Pkcs12, certPwd);

        var importOptions = new ImportCertificateOptions(certName, exportedFromRaw)
        {
            Password = certPwd,
        };

        // Act: Import the certificate
        await certClient.ImportCertificateAsync(importOptions);

        // Assert: A backing secret with the same name should be created
        // Azure Key Vault automatically creates a secret with the same name as the certificate
        // containing the full PFX (with private key) when a certificate is imported
        var backingSecretResponse = await secretClient.GetSecretAsync(certName);
        var backingSecret = backingSecretResponse.Value;

        Assert.NotNull(backingSecret);
        Assert.Equal(certName, backingSecret.Name);
        Assert.NotNull(backingSecret.Value);
        Assert.Equal("application/x-pkcs12", backingSecret.Properties.ContentType);
    }

    [Fact]
    public async Task ImportedPasswordProtectedCertificateBackingSecretHasNoPassword()
    {
        // Arrange: Create and import a password-protected certificate
        var certClient = await fixture.GetClientAsync();
        var secretClient = await fixture.GetSecretClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;
        var originalPassword = fixture.FreshlyGeneratedGuid;

        var x509 = CreateCertificate(certName);

        var exportedFromRaw = x509.Export(X509ContentType.Pkcs12, originalPassword);

        var importOptions = new ImportCertificateOptions(certName, exportedFromRaw)
        {
            Password = originalPassword,
        };

        await certClient.ImportCertificateAsync(importOptions);

        // Act: Get the backing secret directly via SecretClient
        var backingSecretResponse = await secretClient.GetSecretAsync(certName);
        var backingSecret = backingSecretResponse.Value;

        // Assert: The backing secret should contain PFX data
        Assert.NotNull(backingSecret);
        Assert.NotNull(backingSecret.Value);
        Assert.Equal("application/x-pkcs12", backingSecret.Properties.ContentType);

        // Convert base64 secret value to bytes
        var pfxBytes = Convert.FromBase64String(backingSecret.Value);

        // Assert: Load the certificate WITHOUT any password - this proves password was stripped
        // If the password was retained, this would throw CryptographicException
        var loadedCert = X509CertificateLoader.LoadPkcs12(pfxBytes, null);

        Assert.NotNull(loadedCert);
        Assert.True(loadedCert.HasPrivateKey, "Certificate from backing secret should have private key");

        // Verify private key is accessible without the original password
        var privateKey = loadedCert.GetRSAPrivateKey();
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
