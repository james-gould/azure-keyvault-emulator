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
        private HttpClient? _testingClient;

        public async Task<HttpClient> CreateHttpClient(double version, string applicationName = AspireConstants.EmulatorServiceName)
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

            await _notificationService!.WaitForResourceAsync(applicationName, KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));

            return _testingClient;
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

        public async Task DisposeAsync()
        {
            if(_app is not null)
                await _app.DisposeAsync().ConfigureAwait(false);

            if( _testingClient is not null)
                _testingClient.Dispose();
        }
    }
}