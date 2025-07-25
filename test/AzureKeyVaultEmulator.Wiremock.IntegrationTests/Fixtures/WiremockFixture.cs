
using Aspire.Hosting;
using AzureKeyVaultEmulator.Shared.Constants.Orchestration;

namespace AzureKeyVaultEmulator.Wiremock.IntegrationTests.Fixtures;

public sealed class WiremockFixture : IAsyncLifetime
{
    internal readonly TimeSpan _waitPeriod = TimeSpan.FromSeconds(30);
    internal DistributedApplication? _app;
    internal ResourceNotificationService? _notificationService;

    public Task DisposeAsync()
    {
        if (_app != null)
            _app.Dispose();

        if (_notificationService != null)
            _notificationService.Dispose();

        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AzureKeyVaultEmulator_AppHost>(
                [$"--{AspireConstants.Wiremock}"],
                (x, y) => x.DisableDashboard = true
            );

        _app = await builder.BuildAsync();

        _notificationService = _app.Services.GetService<ResourceNotificationService>();

        await _app.StartAsync();
    }

    public async Task<HttpClient> GetHttpClient(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(_notificationService);
        ArgumentNullException.ThrowIfNull(_app);

        await _notificationService.WaitForResourceHealthyAsync(name).WaitAsync(_waitPeriod);

        return _app.CreateHttpClient(name);
    }
}
