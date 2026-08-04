using Aspire.Hosting;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.Shared.Constants.Orchestration;
using AzureKeyVaultEmulator.Shared.Utilities;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures.StaticPort;

/// <summary>
/// <para>Boots <see cref="Projects.AzureKeyVaultEmulator_AppHost"/> twice on the same fixed host port
/// (<see cref="AspireConstants.StaticPortTestPort"/>) with <c>Persist = true</c>, disposing the first
/// application before starting the second.</para>
/// <para>This reproduces the scenario fixed by <see cref="AzureKeyVaultEmulator.Aspire.Hosting.KeyVaultEmulatorOptions.Port"/>:
/// a secret, key and certificate created during the first run embed the vault URI (including the port) in
/// their identifiers. Because the port is pinned, the identifiers persisted in <c>emulator.db</c> remain
/// reachable after a restart, so the second run can query them and download the certificate's private key
/// (the failure reported in issue #449).</para>
/// </summary>
public sealed class StaticPortPersistenceFixture : IAsyncLifetime
{
    private readonly TimeSpan _waitPeriod = TimeSpan.FromSeconds(120);

    private readonly RetryPolicy _clientRetryPolicy = new(
        maxRetries: 5,
        DelayStrategy.CreateExponentialDelayStrategy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)));

    private DistributedApplication? _secondRun;

    /// <summary>The host port the emulator container is pinned to across both runs.</summary>
    public int ConfiguredPort => AspireConstants.StaticPortTestPort;

    /// <summary>The port the vault endpoint was actually exposed on during the first run.</summary>
    public int FirstRunPort { get; private set; }

    /// <summary>The port the vault endpoint was actually exposed on during the second run.</summary>
    public int SecondRunPort { get; private set; }

    public string SecretName { get; } = Guid.NewGuid().Neat();
    public string SecretValue { get; } = Guid.NewGuid().Neat();
    public string KeyName { get; } = Guid.NewGuid().Neat();
    public string CertificateName { get; } = Guid.NewGuid().Neat();

    /// <summary>The secret identifier created during the first run (embeds the configured port).</summary>
    public Uri SecretId { get; private set; } = default!;

    /// <summary>The key identifier created during the first run (embeds the configured port).</summary>
    public Uri KeyId { get; private set; } = default!;

    /// <summary>The certificate's backing secret identifier created during the first run.</summary>
    public Uri CertificateSecretId { get; private set; } = default!;

    /// <summary>The certificate's backing key identifier created during the first run.</summary>
    public Uri CertificateKeyId { get; private set; } = default!;

    /// <summary>Whether the certificate created during the first run exposed a private key.</summary>
    public bool CertificateHadPrivateKeyOnFirstRun { get; private set; }

    // Clients bound to the second (restarted) run.
    public SecretClient SecondRunSecretClient { get; private set; } = default!;
    public KeyClient SecondRunKeyClient { get; private set; } = default!;
    public CertificateClient SecondRunCertificateClient { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var (firstApp, firstEndpoint, firstCredential) = await BootAsync();

        try
        {
            FirstRunPort = firstEndpoint.Port;

            var (secrets, keys, certificates) = BuildClients(firstEndpoint, firstCredential);

            var secret = await secrets.SetSecretAsync(SecretName, SecretValue);
            SecretId = secret.Value.Id;

            var key = await keys.CreateKeyAsync(KeyName, KeyType.Rsa);
            KeyId = key.Value.Id;

            await certificates.StartCreateCertificateAsync(CertificateName, CertificatePolicy.Default);
            var certificate = await certificates.GetCertificateAsync(CertificateName);
            CertificateSecretId = certificate.Value.SecretId;
            CertificateKeyId = certificate.Value.KeyId;

            var firstDownload = await certificates.DownloadCertificateAsync(CertificateName);
            CertificateHadPrivateKeyOnFirstRun = firstDownload.Value.HasPrivateKey;
        }
        finally
        {
            await firstApp.DisposeAsync();
        }

        var (secondApp, secondEndpoint, secondCredential) = await BootAsync();

        _secondRun = secondApp;
        SecondRunPort = secondEndpoint.Port;

        var (secondSecrets, secondKeys, secondCertificates) = BuildClients(secondEndpoint, secondCredential);

        SecondRunSecretClient = secondSecrets;
        SecondRunKeyClient = secondKeys;
        SecondRunCertificateClient = secondCertificates;
    }

    public async Task DisposeAsync()
    {
        if (_secondRun is not null)
            await _secondRun.DisposeAsync().ConfigureAwait(false);
    }

    private async Task<(DistributedApplication app, Uri endpoint, TokenCredential credential)> BootAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AzureKeyVaultEmulator_AppHost>(
                [$"--{AspireConstants.StaticPortTest}"], (x, y) => x.DisableDashboard = true);

        // The Aspire testing harness randomises host ports by default, which would defeat the purpose of
        // this test. Pinning it lets the emulator honour the fixed KeyVaultEmulatorOptions.Port value.
        builder.Configuration["DcpPublisher:RandomizePorts"] = "false";

        var app = await builder.BuildAsync();

        var notifications = app.Services.GetRequiredService<ResourceNotificationService>();

        await app.StartAsync();

        await notifications
            .WaitForResourceHealthyAsync(AspireConstants.EmulatorServiceName)
            .WaitAsync(_waitPeriod);

        var endpoint = app.GetEndpoint(AspireConstants.EmulatorServiceName, "https");

        ArgumentNullException.ThrowIfNull(endpoint);

        var token = await GetBearerTokenAsync(endpoint);

        return (app, endpoint, new EmulatedTokenCredential(token));
    }

    private static async Task<string> GetBearerTokenAsync(Uri endpoint)
    {
        using var client = new HttpClient { BaseAddress = endpoint };

        // The endpoint has just reported healthy, but the very first TLS handshake can still race the
        // container's readiness, so retry a handful of times before giving up.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var response = await client.GetAsync("/token");

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException) when (attempt < 5)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt));
            }
        }
    }

    private (SecretClient secrets, KeyClient keys, CertificateClient certificates) BuildClients(
        Uri endpoint,
        TokenCredential credential)
    {
        var secrets = new SecretClient(endpoint, credential, new SecretClientOptions
        {
            DisableChallengeResourceVerification = true,
            RetryPolicy = _clientRetryPolicy
        });

        var keys = new KeyClient(endpoint, credential, new KeyClientOptions
        {
            DisableChallengeResourceVerification = true,
            RetryPolicy = _clientRetryPolicy
        });

        var certificates = new CertificateClient(endpoint, credential, new CertificateClientOptions
        {
            DisableChallengeResourceVerification = true,
            RetryPolicy = _clientRetryPolicy
        });

        return (secrets, keys, certificates);
    }
}
