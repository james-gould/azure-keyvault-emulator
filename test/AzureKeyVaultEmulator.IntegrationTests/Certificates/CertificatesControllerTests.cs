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
    public async Task CreatingCertificateWithOrganisationWillPersist()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        await Assert.RequestFailsAsync(() => client.GetCertAsync(certName));

        var issuerName = "Self";
        var subject = "CN=keyvault-emulator.com, O=Key Vault Emulator Ltd";

        var policy = new CertificatePolicy(issuerName, subject)
        {
            Exportable = true,
            KeyType = CertificateKeyType.Rsa,
            KeySize = 2048,
            ReuseKey = true,
            ContentType = Azure.Security.KeyVault.Certificates.CertificateContentType.Pkcs12,
            ValidityInMonths = 12

        };

        var operation = await client.StartCreateCertificateAsync(certName, policy);

        await operation.WaitForCompletionAsync();

        Assert.True(operation.HasCompleted);

        var certFromStore = await client.GetCertAsync(certName);

        Assert.Equal(subject, certFromStore.Policy.Subject);
        Assert.Equal(issuerName, certFromStore.Policy.IssuerName);
    }

    [Fact]
    public async Task CreatingCertificateWithTagsWillPersist()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        await Assert.RequestFailsAsync(() => client.GetCertAsync(certName));

        var tagId = fixture.FreshlyGeneratedGuid;

        var operation = await client.StartCreateCertificateAsync(
                    certName,
                    fixture.BasicPolicy,
                    tags: new Dictionary<string, string> { { "id", tagId } }
        );

        await operation.WaitForCompletionAsync();

        Assert.True(operation.HasCompleted);

        var certFromStore = await client.GetCertAsync(certName);

        Assert.NotEmpty(certFromStore.Properties.Tags);
        Assert.Single(certFromStore.Properties.Tags);

        var tagValue = certFromStore.Properties.Tags.FirstOrDefault().Value;

        Assert.NotNull(tagValue);
        Assert.Equal(tagId, tagValue);
    }

    [Fact]
    public async Task GetCertificateByVersionWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var cert = await fixture.CreateCertificateAsync(certName);

        Assert.NotEqual(string.Empty, cert.Properties.Version);

        var versionResponse = await client.GetCertificateVersionAsync(certName, cert.Properties.Version);

        var byVersion = versionResponse.Value;

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

        var updated = await client.UpdateCertificatePolicyAsync(certName, policyToUpdate);

        Assert.NotEqual(cert.Policy, updated);

        Assert.False(updated.Value.Enabled);
    }

    [Fact]
    public async Task GetCertificatePolicyWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var cert = await fixture.CreateCertificateAsync(certName);

        var response = await client.GetCertificatePolicyAsync(certName);

        Assert.NotNull(response.Value);

        var policy = response.Value;

        Assert.NotNull(policy);
        Assert.Equal(cert.Policy.IssuerName, policy.IssuerName);
        Assert.Equal(cert.Policy.Subject, policy.Subject);
        Assert.Equal(cert.Policy.Enabled, policy.Enabled);
    }

    [Fact]
    public async Task CreatingAnIssuerWillPersist()
    {
        var client = await fixture.GetClientAsync();

        var issuerName = fixture.FreshlyGeneratedGuid;

        await Assert.RequestFailsAsync(() => client.GetIssuerAsync(issuerName));

        var issuerConfig = fixture.CreateIssuerConfiguration(issuerName);

        var response = await client.CreateIssuerAsync(issuerConfig);

        Assert.NotNull(response.Value);

        var issuer = response.Value;

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
    public async Task BackingUpAndRestoringCertificateWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var cert = await fixture.CreateCertificateAsync(certName);

        Assert.NotNull(cert);

        var backup = await client.BackupCertificateAsync(certName);

        Assert.NotNull(backup);

        Assert.NotEqual([], backup.Value);

        var restoredResponse = await client.RestoreCertificateBackupAsync(backup.Value);

        Assert.NotNull(restoredResponse.Value);

        var restoredCert = restoredResponse.Value;

        Assert.CertificatesAreEqual(cert, restoredCert);
    }

    [Fact(Skip = "Cyclical tests randomly failing on Github, issue #145")]
    public async Task GetCertificateVersionsListWillCycleLink()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var executionCount = await RequestSetup
            .CreateMultiple(26, 51, i => fixture.CreateCertificateAsync(certName));

        List<CertificateProperties> certs = [];

        await foreach (var cer in client.GetPropertiesOfCertificateVersionsAsync(certName))
            certs.Add(cer);

        Assert.Equal(executionCount, certs.Count);
    }

    [Fact(Skip = "Cyclical tests randomly failing on Github, issue #145")]
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

        var response = await client.ImportCertificateAsync(importOptions);

        Assert.NotNull(response.Value);

        var importedCert = response.Value;

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

        var response = await client.MergeCertificateAsync(mergeOptions);

        Assert.NotNull(response.Value);

        var mergeResult = response.Value;

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

        var response = await client.GetDeletedCertificateAsync(certName);

        Assert.NotNull(response.Value);

        await Assert.RequestFailsAsync(() => client.GetCertAsync(certName));
    }

    [Fact(Skip = @"
        Certificate Operations are currently hardcoded to work in a specific way,
        this functionality requires a refactor of the CertificateOperation class and
        a redesign of the behaviour. The functionality is undocumented in the SDK/API docs,
        but the existing integration tests will capture any regressions.
    ")]
    public async Task DeleteCertificateOperationWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        var createOperation = await client.StartCreateCertificateAsync(certName, fixture.BasicPolicy);

        await createOperation.CancelAsync();

        await Assert.RequestFailsAsync(() => client.GetCertAsync(certName));
    }
}
