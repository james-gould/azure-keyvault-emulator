using AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures.StaticPort;

namespace AzureKeyVaultEmulator.IntegrationTests.Emulator;

/// <summary>
/// <para>End-to-end coverage for the static host port exposed by
/// <see cref="AzureKeyVaultEmulator.Aspire.Hosting.KeyVaultEmulatorOptions.Port"/>.</para>
/// <para>The <see cref="StaticPortPersistenceFixture"/> creates a secret, key and certificate on one run of
/// the emulator, restarts it on the same pinned port with persistence enabled, and exposes clients bound to
/// the restarted instance. These tests assert both that the persisted identifiers embed the configured port
/// (the internal data that makes the fix necessary) and that the data remains usable after the restart -
/// including downloading the certificate's private key, which regressed in issue #449.</para>
/// </summary>
public sealed class EmulatorStaticPortPersistenceTests(StaticPortPersistenceFixture fixture)
    : IClassFixture<StaticPortPersistenceFixture>
{
    [Fact]
    public void ConfiguredPortIsAppliedToBothRuns()
    {
        Assert.Equal(fixture.ConfiguredPort, fixture.FirstRunPort);
        Assert.Equal(fixture.ConfiguredPort, fixture.SecondRunPort);
    }

    [Fact]
    public void PersistedSecretIdentifierEmbedsConfiguredPort()
    {
        Assert.Equal(fixture.ConfiguredPort, fixture.SecretId.Port);
    }

    [Fact]
    public void PersistedKeyIdentifierEmbedsConfiguredPort()
    {
        Assert.Equal(fixture.ConfiguredPort, fixture.KeyId.Port);
    }

    [Fact]
    public void PersistedCertificateIdentifiersEmbedConfiguredPort()
    {
        Assert.Equal(fixture.ConfiguredPort, fixture.CertificateSecretId.Port);
        Assert.Equal(fixture.ConfiguredPort, fixture.CertificateKeyId.Port);
    }

    [Fact]
    public async Task SecretIsQueryableAfterRestartWithStableIdentifier()
    {
        var secret = await fixture.SecondRunSecretClient.GetSecretAsync(fixture.SecretName);

        Assert.Equal(fixture.SecretValue, secret.Value.Value);
        Assert.Equal(fixture.SecretId, secret.Value.Id);
        Assert.Equal(fixture.ConfiguredPort, secret.Value.Id.Port);
    }

    [Fact]
    public async Task KeyIsQueryableAfterRestartWithStableIdentifier()
    {
        var key = await fixture.SecondRunKeyClient.GetKeyAsync(fixture.KeyName);

        Assert.Equal(fixture.KeyId, key.Value.Id);
        Assert.Equal(fixture.ConfiguredPort, key.Value.Id.Port);
    }

    [Fact]
    public async Task CertificateIsQueryableAfterRestartWithStableIdentifiers()
    {
        var certificate = await fixture.SecondRunCertificateClient.GetCertificateAsync(fixture.CertificateName);

        Assert.Equal(fixture.CertificateSecretId, certificate.Value.SecretId);
        Assert.Equal(fixture.CertificateKeyId, certificate.Value.KeyId);
        Assert.Equal(fixture.ConfiguredPort, certificate.Value.SecretId.Port);
    }

    [Fact]
    public async Task CertificatePrivateKeyIsDownloadableAfterRestart()
    {
        Assert.True(
            fixture.CertificateHadPrivateKeyOnFirstRun,
            "the certificate created during the first run should expose a private key");

        var certificate = await fixture.SecondRunCertificateClient.DownloadCertificateAsync(fixture.CertificateName);

        Assert.True(
            certificate.Value.HasPrivateKey,
            "the certificate downloaded after a restart should still expose a private key");
    }
}
