using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
//using Azure.Identity;
//using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.IntegrationTests.Certificates.Helpers;
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
    public async Task ImportingCertificateChainWillPersistAllCertificates()
    {
        var client = await fixture.GetClientAsync();
        var secretClient = await fixture.GetSecretClientAsync();

        //var uri = new Uri("https://azure-keyvault-emulator.vault.azure.net/");

        //var client = new CertificateClient(uri, new DefaultAzureCredential());
        //var secretClient = new SecretClient(uri, new DefaultAzureCredential());

        var certName = fixture.FreshlyGeneratedGuid;
        var certPwd = fixture.FreshlyGeneratedGuid;

        var cName = fixture.FreshlyGeneratedGuid;

        var allCerts = MultiCertGenerator.Generate(cName);

        var importOptions = new ImportCertificateOptions(certName, allCerts.PfxBytes);

        var importResult = await client.ImportCertificateAsync(importOptions);

        var backingSecret = await secretClient.GetSecretAsync(certName);

        Assert.NotNull(importResult.Value);
        Assert.NotNull(backingSecret.Value);

        var base64Chain = Convert.FromBase64String(backingSecret.Value.Value);

        // Password is always empty for imports. PKCS#12 exports require a password
        // This matches Azure exporting a full chain too.
        var chainCollection = X509CertificateLoader.LoadPkcs12Collection(base64Chain, "");

        // Assert that the generated count of certificates matches the count of the backing secret contents, parsed as PKCS#12
        Assert.Equal(allCerts.All.Count(), chainCollection.Count);

        Assert.NotNull(chainCollection.OfType<X509Certificate2>().SingleOrDefault(x => x.HasPrivateKey));
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
    public async Task ImportingPasswordProtectedCertificateStripsPassword()
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

        // Act: DownloadCertificateAsync is a convenience method in Azure SDK that internally
        // retrieves the backing secret (via SecretClient) and returns the full certificate with private key.
        // This is different from GetCertificateAsync which only returns public cert + metadata.
        var downloadedCertificateResponse = await client.DownloadCertificateAsync(certName);
        var downloadedCertificate = downloadedCertificateResponse.Value;

        // Assert: The downloaded certificate should be usable without the original password
        // Azure Key Vault strips the password from imported certificates when storing in backing secret
        Assert.NotNull(downloadedCertificate);
        Assert.True(downloadedCertificate.HasPrivateKey, "DownloadCertificateAsync should return certificate with private key (from backing secret)");

        // Verify private key is accessible without the original password
        // This would fail if the password was retained in the backing secret
        var privateKey = downloadedCertificate.GetRSAPrivateKey();
        Assert.NotNull(privateKey);
    }

    [Fact]
    public async Task GettingCertificateDoesNotIncludePrivateKey()
    {
        // Arrange: Import a certificate with private key
        var certClient = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;
        var certPwd = fixture.FreshlyGeneratedGuid;

        var x509 = CreateCertificate(certName);

        var exportedFromRaw = x509.Export(X509ContentType.Pkcs12, certPwd);

        var importOptions = new ImportCertificateOptions(certName, exportedFromRaw)
        {
            Password = certPwd,
        };

        await certClient.ImportCertificateAsync(importOptions);

        // Act: Get the certificate via CertificateClient (NOT SecretClient)
        var certResponse = await certClient.GetCertificateAsync(certName);
        var cert = certResponse.Value;

        // Assert: The certificate from CertificateClient should NOT have private key
        // Azure Key Vault's Certificates API only returns public certificate + metadata
        // Private key is only available via SecretClient (backing secret)
        Assert.NotNull(cert);
        Assert.NotNull(cert.Cer);
        Assert.NotEmpty(cert.Cer);

        // Load the certificate from the Cer property (raw X.509 certificate bytes - DER encoded)
        var loadedCert = X509CertificateLoader.LoadCertificate(cert.Cer);

        Assert.NotNull(loadedCert);
        Assert.False(loadedCert.HasPrivateKey, "Certificate from CertificateClient.GetCertificateAsync should NOT have private key");
    }

    [Fact]
    public async Task ImportingCertificateCreatesBackingSecret()
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
    public async Task ImportingPasswordProtectedCertificateBackingSecretHasNoPassword()
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
