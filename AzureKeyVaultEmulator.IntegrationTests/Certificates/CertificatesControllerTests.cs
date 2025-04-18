﻿using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;
using AzureKeyVaultEmulator.IntegrationTests.Extensions;
using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.Shared.Constants;

namespace AzureKeyVaultEmulator.IntegrationTests.Certificates;

public class CertificatesControllerTests(CertificatesTestingFixture fixture)
    : IClassFixture<CertificatesTestingFixture>
{
    [Fact]
    public async Task NotWaitingForOperationWillThrow()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        await Assert.ThrowsRequestFailedAsync(() => client.GetCertAsync(certName));

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

        await Assert.ThrowsRequestFailedAsync(() => client.GetCertificateAsync(certName));

        var operation = await client.StartCreateCertificateAsync(certName, fixture.BasicPolicy, enabled: true);

        Assert.NotNull(operation);

        var response = await operation.UpdateStatusAsync();

        Assert.Equal((int)HttpStatusCode.OK, response.Status);

        var certificateFromStore = await client.GetCertAsync(certName);

        Assert.Equal(certName, certificateFromStore.Name);
    }

    [Fact]
    public async Task WaitForCompletionWillCompleteCreationOperation()
    {
        var client = await fixture.GetClientAsync();

        var certName = fixture.FreshlyGeneratedGuid;

        await Assert.ThrowsRequestFailedAsync(() => client.GetCertificateAsync(certName));

        var operation = await client.StartCreateCertificateAsync(certName, fixture.BasicPolicy, enabled: true);

        await operation.WaitForCompletionAsync();

        Assert.True(operation.HasCompleted);

        var certificateFromStore = await client.GetCertAsync(certName);

        Assert.Equal(certName, certificateFromStore.Name);
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

        Assert.NotNull(cert?.Policy?.SubjectAlternativeNames);

        var certSan = cert!.Policy.SubjectAlternativeNames!;

        Assert.Contains(email, certSan.Emails);
        Assert.Contains(ip, certSan.DnsNames);
        Assert.Contains(principal, certSan.UserPrincipalNames);
    }
}
