using Asp.Versioning;
using Asp.Versioning.Http;
using AzureKeyVaultEmulator.Shared.Constants;
using Microsoft.Extensions.Hosting;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures
{
    public sealed class EmulatorTestingFixture : IAsyncLifetime
    {
        private DistributedApplication _app;

        public HttpClient CreateHttpClient(double version)
        {
            // Requires extension of testing library to include this
            var opt = new ApiVersionHandler(new QueryStringApiVersionWriter(), new ApiVersion(version));

            return _app.CreateHttpClient(AspireConstants.EmulatorServiceName);
        }

        public async Task InitializeAsync()
        {
            var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AzureKeyVaultEmulator_AppHost>();

            builder.Services.ConfigureHttpClientDefaults(c =>
            {
                c.AddStandardResilienceHandler();
            });

            _app = await builder.BuildAsync();

            await _app.StartAsync();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await _app.StopAsync();
            if (_app is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _app.Dispose();
            }
        }
    }
}