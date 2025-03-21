using Asp.Versioning;
using Asp.Versioning.Http;
using Aspire.Hosting;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.Shared.Constants;
using IdentityModel.Client;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures
{
    public sealed class EmulatorTestingFixture : IAsyncLifetime
    {
        private readonly TimeSpan _waitPeriod = TimeSpan.FromSeconds(30);
        private DistributedApplication? _app;
        private ResourceNotificationService? _notificationService;

        private SecretClient? _secretClient;
        private HttpClient? _testingClient;

        public async Task InitializeAsync()
        {
            var builder = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.AzureKeyVaultEmulator_AppHost>([], (x, y) => x.DisableDashboard = true);

            _app = await builder.BuildAsync();

            _notificationService = _app.Services.GetService<ResourceNotificationService>();

            await _app.StartAsync();
        }

        public async Task<SecretClient> GetSecretClientAsync(string applicationName = AspireConstants.EmulatorServiceName)
        {
            if (_secretClient is not null)
                return _secretClient;

            var vaultEndpoint = _app!.GetEndpoint(applicationName);

            await _notificationService!.WaitForResourceAsync(applicationName).WaitAsync(_waitPeriod);

            var options = new SecretClientOptions
            {
                DisableChallengeResourceVerification = true
            };

            var emulatedBearerToken = await GetBearerToken();

            var cred = new EmulatedTokenCredential(emulatedBearerToken);

            _secretClient = new SecretClient(vaultEndpoint, cred, options);

            return _secretClient;
        }

        public async Task<HttpClient> CreateHttpClient(double version = 7.5, string applicationName = AspireConstants.EmulatorServiceName)
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

        public async Task<string> GetBearerToken()
        {
            if (_testingClient is null)
                _testingClient = await CreateHttpClient();

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