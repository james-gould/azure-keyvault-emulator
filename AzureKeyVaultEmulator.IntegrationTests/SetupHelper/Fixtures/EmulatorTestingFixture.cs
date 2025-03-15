using Asp.Versioning;
using Asp.Versioning.Http;
using Aspire.Hosting;
using AzureKeyVaultEmulator.Shared.Constants;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures
{
    public sealed class EmulatorTestingFixture : IAsyncLifetime
    {
        private DistributedApplication? _app;
        private ResourceNotificationService? _notificationService;

        public async Task<HttpClient> CreateHttpClient(double version, string applicationName = AspireConstants.EmulatorServiceName)
        {
            // Requires extension of testing library to include this
            var opt = new ApiVersionHandler(new QueryStringApiVersionWriter(), new ApiVersion(version));

            var client = _app!.CreateHttpClient(applicationName);

            await _notificationService!.WaitForResourceAsync(applicationName, KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));

            return client;
        }

        public async Task InitializeAsync()
        {
            var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AzureKeyVaultEmulator_AppHost>();

            builder.Services.ConfigureHttpClientDefaults(c =>
            {
                c.AddStandardResilienceHandler();
            });

            _app = await builder.BuildAsync();

            _notificationService = _app.Services.GetService<ResourceNotificationService>();

            await _app.StartAsync();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            if(_app is not null)
                await _app.DisposeAsync().ConfigureAwait(false);
        }
    }
}