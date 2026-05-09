using Asp.Versioning;
using Asp.Versioning.Http;
using Aspire.Hosting;
using Azure.Core;
using Azure.Core.Pipeline;
using AzureKeyVaultEmulator.Shared.Constants.Orchestration;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures.Seeding;

/// <summary>
/// <para>Stripped down counterpart to <see cref="KeyVaultClientTestingFixture{TClient}"/> used to verify
/// the seeding functionality exposed by the AppHost.</para>
/// <para>Boots <see cref="Projects.AzureKeyVaultEmulator_AppHost"/> with the
/// <c>--<see cref="AspireConstants.SeedingTest"/></c> flag so the seeding code path is taken.</para>
/// </summary>
/// <typeparam name="TClient">The Azure SDK client type used by the derived fixture.</typeparam>
public abstract class SeedingTestingFixture<TClient> : IAsyncLifetime
    where TClient : class
{
    internal readonly TimeSpan _waitPeriod = TimeSpan.FromSeconds(30);
    internal DistributedApplication? _app;
    internal ResourceNotificationService? _notificationService;

    internal readonly RetryPolicy _clientRetryPolicy = new(
        maxRetries: 5,
        DelayStrategy.CreateExponentialDelayStrategy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(100)));

    private ClientSetupVM? _setupModel;

    private HttpClient? _testingClient;
    private string _bearerToken = string.Empty;

    public abstract ValueTask<TClient> GetClientAsync();

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AzureKeyVaultEmulator_AppHost>(
                [$"--{AspireConstants.SeedingTest}"], (x, y) => x.DisableDashboard = true
            );

        _app = await builder.BuildAsync();

        _notificationService = _app.Services.GetService<ResourceNotificationService>();

        await _app.StartAsync();
    }

    public async ValueTask<HttpClient> CreateHttpClient(double version = 7.5, string applicationName = AspireConstants.EmulatorServiceName)
    {
        if (_testingClient is not null)
            return _testingClient;

        var opt = new ApiVersionHandler(new QueryStringApiVersionWriter(), new ApiVersion(version))
        {
            InnerHandler = new HttpClientHandler()
        };

        await _notificationService!.WaitForResourceHealthyAsync(applicationName).WaitAsync(_waitPeriod);

        var endpoint = _app!.GetEndpoint(applicationName);

        ArgumentNullException.ThrowIfNull(endpoint);

        _testingClient = new HttpClient(opt)
        {
            BaseAddress = endpoint
        };

        return _testingClient;
    }

    internal async ValueTask<ClientSetupVM> GetClientSetupModelAsync(string applicationName = AspireConstants.EmulatorServiceName)
    {
        if (_setupModel is not null)
            return _setupModel;

        var vaultEndpoint = _app!.GetEndpoint(applicationName);

        ArgumentNullException.ThrowIfNull(vaultEndpoint);

        await _notificationService!.WaitForResourceHealthyAsync(applicationName).WaitAsync(_waitPeriod);

        var emulatedBearerToken = await GetBearerTokenAsync();

        var cred = new EmulatedTokenCredential(emulatedBearerToken);

        return _setupModel = new ClientSetupVM(vaultEndpoint, cred);
    }

    public async ValueTask<string> GetBearerTokenAsync()
    {
        if (!string.IsNullOrEmpty(_bearerToken))
            return _bearerToken;

        _testingClient ??= await CreateHttpClient();

        var response = await _testingClient.GetAsync("/token");

        response.EnsureSuccessStatusCode();

        return _bearerToken = await response.Content.ReadAsStringAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync().ConfigureAwait(false);

        _testingClient?.Dispose();
    }
}
