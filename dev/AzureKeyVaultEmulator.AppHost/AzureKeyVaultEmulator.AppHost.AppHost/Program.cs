using AzureKeyVaultEmulator.AppHost;
using AzureKeyVaultEmulator.Shared.Constants.Orchestration;
using WireMock.Server;

var isWiremockTestRunning = args.GetFlag(AspireConstants.Wiremock);

var builder = DistributedApplication.CreateBuilder();

var keyVault = builder.AddProject<Projects.AzureKeyVaultEmulator>(AspireConstants.EmulatorServiceName);

if (isWiremockTestRunning)
{
    var wiremockServer = WireMockServer
        .Start(useSSL: true);

    wiremockServer.Given(WireMock.RequestBuilders.Request.Create()
        .WithPath(WiremockConstants.EndpointPath)
        .UsingGet())
        .RespondWith(
             WireMock.ResponseBuilders.Response.Create()
            .WithStatusCode(200)
            .WithBody(WiremockConstants.ConnectivityResponse)
        );

    var webApi = builder
        .AddProject<Projects.WebApiWithEmulator_DebugHelper>(AspireConstants.DebugHelper)
        .WithEnvironment($"ConnectionStrings__{AspireConstants.EmulatorServiceName}", keyVault.GetEndpoint("https"))
        .WithReference(keyVault)
        .WaitFor(keyVault);

    webApi.WithEnvironment(AspireConstants.Wiremock, wiremockServer.Url);
}

builder.Build().Run();
