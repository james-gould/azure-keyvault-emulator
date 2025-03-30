using Asp.Versioning;
using Asp.Versioning.Http;
using Aspire.Hosting;
using AzureKeyVaultEmulator.Shared.Constants;
using IdentityModel.Client;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures
{
    public class EmulatorTestingFixture : IAsyncLifetime
    {
        internal readonly TimeSpan _waitPeriod = TimeSpan.FromSeconds(30);
        internal DistributedApplication? _app;
        internal ResourceNotificationService? _notificationService;

        private HttpClient? _testingClient;
        private string _bearerToken = string.Empty;

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

            await _notificationService!.WaitForResourceAsync(applicationName, KnownResourceStates.Running).WaitAsync(_waitPeriod);

            return _testingClient;
        }

        public async ValueTask<string> GetBearerToken()
        {
            if (_testingClient is null)
                _testingClient = await CreateHttpClient();

            if(!string.IsNullOrEmpty(_bearerToken))
                return _bearerToken;

            var response = await _testingClient.GetAsync("/token");

            response.EnsureSuccessStatusCode();

            var jwt = await response.Content.ReadAsStringAsync();

            _testingClient.SetBearerToken(jwt);

            return jwt;
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