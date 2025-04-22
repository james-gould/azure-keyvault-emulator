using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;
using AzureKeyVaultEmulator.IntegrationTests.Extensions;
using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.Shared.Constants;
using AzureKeyVaultEmulator.IntegrationTests.SetupHelper;

namespace AzureKeyVaultEmulator.IntegrationTests.Certificates;

public class CertificatesControllerTests(CertificatesTestingFixture fixture)
    : IClassFixture<CertificatesTestingFixture>
{
    [Fact]
    public async Task NotWaitingForOperationWillThrow()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        await Assert.RequestFailsAsync(() => client.GetCertAsync(certName));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var cert = (await client.StartCreateCertificateAsync(certName, fixture.BasicPolicy, enabled: true)).Value;
        });
    }

    [Fact]
    public async Task EvaulatingCertificateOperationWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        await Assert.RequestFailsAsync(() => client.GetCertificateAsync(certName));

        var operation = await client.StartCreateCertificateAsync(certName, fixture.BasicPolicy, enabled: true);

        Assert.NotNull(operation);

        var response = await operation.UpdateStatusAsync();

        Assert.Equal((int)HttpStatusCode.OK, response.Status);

        var certificateFromStore = await client.GetCertAsync(certName);

        Assert.Equal(certName, certificateFromStore.Name);
    }

    [Fact]
    public async Task NewlyCreatedCertificateWillHaveVersion()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var cert = await fixture.CreateCertificateAsync(certName);

        Assert.NotNull(cert.Properties.Version);
        Assert.NotEqual(string.Empty, cert.Properties.Version);
    }

    [Fact]
    public async Task WaitForCompletionWillCompleteCreationOperation()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        await Assert.RequestFailsAsync(() => client.GetCertificateAsync(certName));

        var operation = await client.StartCreateCertificateAsync(certName, fixture.BasicPolicy, enabled: true);

        await operation.WaitForCompletionAsync();

        Assert.True(operation.HasCompleted);

        var certificateFromStore = await client.GetCertAsync(certName);

        Assert.Equal(certName, certificateFromStore.Name);
    }

    [Fact]
    public async Task GetCertificateByVersionWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var cert = await fixture.CreateCertificateAsync(certName);

        Assert.NotEqual(string.Empty, cert.Properties.Version);

        var byVersion = await client.GetCertVersionAsync(certName, cert.Properties.Version);

        Assert.CertificatesAreEqual(byVersion, cert);
    }

    [Fact]
    public async Task AddingCustomSubjectsToCertificateWillPersist()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var email = "emulator@keyvault.net";
        var ip = "127.0.0.1";
        var principal = "Emulator";

        var sans = new SubjectAlternativeNames();

        sans.Emails.Add(email);
        sans.UserPrincipalNames.Add(principal);
        sans.DnsNames.Add(ip);

        var policy = new CertificatePolicy(AuthConstants.EmulatorIss, sans)
        {
            KeySize = 2048,
            ContentType = Azure.Security.KeyVault.Certificates.CertificateContentType.Pkcs12
        };

        var operation = await client.StartCreateCertificateAsync(certName, policy);

        await operation.WaitForCompletionAsync();

        var cert = await client.GetCertAsync(certName);

        Assert.NotNull(cert.Policy.SubjectAlternativeNames);

        Assert.Equal(AuthConstants.EmulatorIss, cert.Policy.IssuerName);

        var certSan = cert!.Policy.SubjectAlternativeNames!;

        Assert.Contains(email, certSan.Emails);
        Assert.Contains(ip, certSan.DnsNames);
        Assert.Contains(principal, certSan.UserPrincipalNames);
    }

    [Fact]
    public async Task UpdatingCertificateWillPersistChange()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var cert = await fixture.CreateCertificateAsync(certName);

        Assert.True(cert.Policy.Enabled);

        var policyToUpdate = fixture.BasicPolicy;

        policyToUpdate.Enabled = false;

        var updatedPolicy = await client.UpdateCertificatePolicyWithResponseAsync(certName, policyToUpdate);

        Assert.NotEqual(cert.Policy, updatedPolicy);

        Assert.False(updatedPolicy.Enabled);
    }

    [Fact]
    public async Task GetCertificatePolicyWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var cert = await fixture.CreateCertificateAsync(certName);

        var certPolicy = await client.GetCertAsync(certName);

        Assert.NotNull(certPolicy);

        var policy = certPolicy.Policy;

        Assert.NotNull(policy);

        Assert.Equal(cert.Policy.IssuerName, policy.IssuerName);
        Assert.Equal(cert.Policy.Subject, policy.Subject);
        Assert.Equal(cert.Policy.Enabled, policy.Enabled);
    }

    [Fact]
    public async Task CreatingAnIssuerWillPersist()
    {
        var client = await fixture.GetClientAsync();

        //var issuerName = fixture.FreshlyGeneratedGuid;
        var issuerName = "testingNonGuid";

        await Assert.RequestFailsAsync(() => client.GetIssuerAsync(issuerName));

        var issuerConfig = fixture.CreateIssuerConfiguration(issuerName);

        var issuer = await client.CreateIssuerWithResponseAsync(issuerConfig);

        Assert.NotNull(issuer);

        // Can't use top level Equivalent due to Id being set at create time, it's null in the setup config.
        Assert.Equivalent(issuerConfig.AdministratorContacts, issuer.AdministratorContacts);
        Assert.Equal(issuerConfig.Name, issuer.Name);
        Assert.Equal(issuerConfig.AccountId, issuer.AccountId);
        Assert.Equal(issuerConfig.Password, issuer.Password);
        Assert.Equal(issuerConfig.Enabled, issuer.Enabled);
        Assert.Equal(issuerConfig.Provider, issuer.Provider);

        Assert.NotEqual(issuerConfig.Id, issuer.Id);
    }

    [Fact]
    public async Task GetCertificateIssuerWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var issuerName = fixture.FreshlyGeneratedGuid;
        var certName = fixture.FreshlyGeneratedGuid;

        await Assert.RequestFailsAsync(() => client.GetIssuerAsync(issuerName));

        var issuerConfig = fixture.CreateIssuerConfiguration(issuerName);

        var createdResponse = await client.CreateIssuerAsync(issuerConfig);

        var issuer = await client.GetIssuerWithResponseAsync(issuerName);

        Assert.NotNull(issuer);

        Assert.IssuersAreEqual(issuerConfig, issuer);
    }

    [Fact]
    public async Task BackingUpAndRestoringCertificateWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var cert = await fixture.CreateCertificateAsync(certName);

        Assert.NotNull(cert);

        var backupCert = await client.BackupCertificateWithResponseAsync(certName);

        Assert.NotNull(backupCert);

        Assert.NotEqual([], backupCert);

        var restoredCert = await client.RestoreCertificateBackupWithResponseAsync(backupCert);

        Assert.NotNull(restoredCert);

        Assert.CertificatesAreEqual(cert, restoredCert);
    }

    [Fact]
    public async Task GetCertificateVersionsListWillCycleLink()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var executionCount = await RequestSetup
            .CreateMultiple(26, 51, i => fixture.CreateCertificateAsync(certName));

        List<CertificateProperties> certs = [];

        await foreach (var cer in client.GetPropertiesOfCertificateVersionsAsync(certName))
            certs.Add(cer);

        Assert.Equal(executionCount + 1, certs.Count);
    }

    [Fact]
    public async Task GetCertificatesListWillCycleLink()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var executionCount = await RequestSetup
            .CreateMultiple(26, 51, i => fixture.CreateCertificateAsync(certName));

        List<CertificateProperties> certs = [];

        await foreach (var cer in client.GetPropertiesOfCertificatesAsync())
            certs.Add(cer);

        // Unless we run sequentially (times out GH actions), we can't assert anything useful here
        // As long as the requests succeed (ie doesn't timeout) and doesn't 404, we're fine.
        Assert.NotEmpty(certs);
    }

    //[Fact(Skip = "404 issue from CertificateClient again, underlying endpoint/functionality works fine. See iss #106")]
    [Fact]
    public async Task ImportingCertificateWillPersistInStore()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var cert = await fixture.CreateCertificateAsync(certName);

        var certData = cert.Cer;

        Assert.NotEmpty(certData);

        var importOptions = new ImportCertificateOptions(certName, certData)
        {
            Enabled = true,
            Policy = CertificatePolicy.Default
        };

        var importedCert = await client.ImportCertWithResponseAsync(importOptions);

        Assert.NotNull(importedCert);

        Assert.CertificatesAreEqual(importedCert, cert, fromGet: false);
    }

    [Fact(Skip = "Pending changes to key pair in certificates")]
    public async Task MergingCertificateWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var baseCertName = fixture.FreshlyGeneratedGuid;
        var mergingCertName = fixture.FreshlyGeneratedGuid;

        var baseCert = await fixture.CreateCertificateAsync(baseCertName);

        var mergingCert = Encoding.UTF8.GetBytes(fixture.X509CertificateWithPrivateKey);

        var mergeOptions = new MergeCertificateOptions(baseCertName, [mergingCert])
        {
            Enabled = true
        };

        var mergeResult = await client.MergeCertWithResponseAsync(mergeOptions);

        Assert.NotNull(mergeResult);

        // Being lazy, probably a better way of asserting the merge went through.
        Assert.Throws<Exception>(() => Assert.CertificatesAreEqual(baseCert, mergeResult));
    }

    [Fact]
    public async Task DeleteCertificateWillRemoveFromMainStore()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var cert = await fixture.CreateCertificateAsync(certName);

        Assert.NotNull(cert);

        await Assert.RequestFailsAsync(() => client.GetDeletedCertificateAsync(certName));

        var deleteOp = await client.StartDeleteCertificateAsync(certName);

        Assert.NotNull(deleteOp);

        await deleteOp.WaitForCompletionAsync();

        var deletedCert = await client.MergeDeletedCertWithResponseAsync(certName);

        Assert.NotNull(deletedCert);

        await Assert.RequestFailsAsync(() => client.GetCertAsync(certName));
    }
}
