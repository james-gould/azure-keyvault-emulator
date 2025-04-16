using System.Collections.Concurrent;
using Asp.Versioning;
using Asp.Versioning.Http;
using Aspire.Hosting;
using Azure.Core;
using Azure.Core.Pipeline;
using AzureKeyVaultEmulator.Shared.Constants;
using IdentityModel.Client;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures;

public class EmulatorTestingFixture : IAsyncLifetime
{
    internal readonly TimeSpan _waitPeriod = TimeSpan.FromSeconds(30);
    internal DistributedApplication? _app;
    internal ResourceNotificationService? _notificationService;

    internal readonly RetryPolicy _clientRetryPolicy = new(
        maxRetries: 5,
        DelayStrategy.CreateExponentialDelayStrategy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)));
        
    private ClientSetupVM? _setupModel;

    private HttpClient? _testingClient;
    private string _bearerToken = string.Empty;

    private CancellationTokenSource _cancellationTokenSource = new(TimeSpan.FromSeconds(30));
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    // Used to ensure no duplicates are used during high concurrency testing
    private readonly ConcurrentBag<string> _spentGuids = [];
    public string FreshlyGeneratedGuid => GetCleanGuid();

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AzureKeyVaultEmulator_AppHost>([], (x, y) => x.DisableDashboard = true);

        _app = await builder.BuildAsync();

        _notificationService = _app.Services.GetService<ResourceNotificationService>();

        await _app.StartAsync();
    }

    public async ValueTask<HttpClient> CreateHttpClient(double version = 7.5, string applicationName = AspireConstants.EmulatorServiceName)
    {
        if (_testingClient is not null)
            return _testingClient;

    public async ValueTask<HttpClient> CreateHttpClient(double version = 7.5, string applicationName = AspireConstants.EmulatorServiceName)
    {
        if (_testingClient is not null)
            return _testingClient;

        // Requires extension of testing library to include this
        var opt = new ApiVersionHandler(new QueryStringApiVersionWriter(), new ApiVersion(version))
        {
            InnerHandler = new HttpClientHandler()
        };

        var endpoint = _app!.GetEndpoint(applicationName);

        _testingClient = new HttpClient(opt)
        {
            BaseAddress = endpoint
        };

        await _notificationService!.WaitForResourceHealthyAsync(applicationName).WaitAsync(_waitPeriod);

        return _testingClient;
    }

    internal async ValueTask<ClientSetupVM> GetClientSetupModelAsync(string applicationName = AspireConstants.EmulatorServiceName)
    {
        if (_setupModel is not null)
            return _setupModel;

        var vaultEndpoint = _app!.GetEndpoint(applicationName);

        await _notificationService!.WaitForResourceAsync(applicationName).WaitAsync(_waitPeriod);

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

        _bearerToken = await response.Content.ReadAsStringAsync();

    public async ValueTask<string> GetBearerTokenAsync()
    {
        if (!string.IsNullOrEmpty(_bearerToken))
            return _bearerToken;

        _testingClient ??= await CreateHttpClient();

        var response = await _testingClient.GetAsync("/token");

        response.EnsureSuccessStatusCode();

        _bearerToken = await response.Content.ReadAsStringAsync();

        _testingClient.SetBearerToken(_bearerToken);

        return _bearerToken;
    }

    private string GetCleanGuid()
    {
        var guid = Guid.NewGuid().ToString("n");

        var exists = _spentGuids.FirstOrDefault(x => x.Equals(guid, StringComparison.InvariantCultureIgnoreCase));

        if (exists is not null)
            throw new InvalidOperationException($"GUID clash during generation");

        _spentGuids.Add(guid);

        return guid;
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync().ConfigureAwait(false);

        if (_testingClient is not null)
            _testingClient.Dispose();
    }
}
