using Asp.Versioning;
using Asp.Versioning.Http;
using Microsoft.Extensions.Hosting;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper.Fixtures
{
    public sealed class EmulatorTestingFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly IHost _app;

        public EmulatorTestingFixture()
        {
            var options = new DistributedApplicationOptions { AssemblyName = typeof(EmulatorTestingFixture).Assembly.FullName, DisableDashboard = true };
            var builder = DistributedApplication.CreateBuilder(options);

            _app = builder.Build();
        }

        public HttpClient CreateHttpClient(double version)
        {
            var opt = new ApiVersionHandler(new QueryStringApiVersionWriter(), new ApiVersion(version));

            return CreateDefaultClient(opt);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Area to configure IHost if required.

            return base.CreateHost(builder);
        }

        public async Task InitializeAsync()
        {
            await _app.StartAsync();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await DisposeAsync();
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