using AzureKeyVaultEmulator.AppHost;
using AzureKeyVaultEmulator.Aspire.Hosting;
using AzureKeyVaultEmulator.Shared.Constants.Orchestration;
using WireMock.Server;

var isWiremockTestRunning = args.GetFlag(AspireConstants.Wiremock);

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

if (isWiremockTestRunning)
{
    var wiremockServer = WireMockServer
        .Start(useSSL: true);

    wiremockServer.Given(WireMock.RequestBuilders.Request.Create()
        .WithPath(WiremockConstants.EndpointPath)
        .UsingGet())
        .RespondWith(WireMock.ResponseBuilders.Response.Create()
        .WithStatusCode(200)
        .WithBody(WiremockConstants.ConnectivityResponse));

    webApi.WithEnvironment(AspireConstants.Wiremock, wiremockServer.Url);
}

builder.Build().Run();
