using AzureKeyVaultEmulator.Aspire.Hosting;
using AzureKeyVaultEmulator.Shared.Constants.Orchestration;
using WireMock.Server;

var wiremockFlag = args.FirstOrDefault(x => x.Contains(AspireConstants.Wiremock, StringComparison.InvariantCultureIgnoreCase));
var isWiremockTestRunning = !string.IsNullOrEmpty(wiremockFlag);

var builder = DistributedApplication.CreateBuilder();

var keyVault = builder
    .AddAzureKeyVault(AspireConstants.EmulatorServiceName)
    .RunAsEmulator(
        new KeyVaultEmulatorOptions
        {
            UseDotnetDevCerts = isWiremockTestRunning
            //Lifetime = ContainerLifetime.Persistent
        }
    );

var webApi = builder
    .AddProject<Projects.WebApiWithEmulator_DebugHelper>(AspireConstants.DebugHelper)
    .WithReference(keyVault)
    .WaitFor(keyVault);

if (string.IsNullOrEmpty(wiremockFlag))
{
    var wiremockServer = WireMockServer
        .Start(useSSL: true);

    wiremockServer.Given(WireMock.RequestBuilders.Request.Create()
        .WithPath(WiremockConstants.EndpointName)
        .UsingGet())
        .RespondWith(WireMock.ResponseBuilders.Response.Create()
        .WithStatusCode(200)
        .WithBody(WiremockConstants.ConnectivityResponse));

    webApi.WithEnvironment(AspireConstants.Wiremock, wiremockServer.Url);
}

builder.Build().Run();
