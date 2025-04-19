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

        await Assert.ThrowsRequestFailedAsync(() => client.GetCertificateAsync(certName));

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
    public async Task GetCertificateIssuerWillSucceed()
    {
        var client = await fixture.GetClientAsync();

        var issuerName = fixture.FreshlyGeneratedGuid;

        var issuer = await client.GetIssuerAsync(issuerName);

        Assert.NotNull(issuer);
    }
}
