using AzureKeyVaultEmulator.Aspire.Hosting;
using AzureKeyVaultEmulator.Shared.Constants;

var builder = DistributedApplication.CreateBuilder(args);

// Integration tests seem to fail due to an unavailable endpoint when referencing another project?
// Haven't seen it before, likely something wonky with the Client library and creating of an Aspire resource
var useDeployedDockerContainer = true;

if (useDeployedDockerContainer)
{
    var keyVault = builder
        .AddAzureKeyVault(AspireConstants.EmulatorServiceName)
        .RunAsEmulator();

    var webApi = builder
        .AddProject<Projects.WebApiWithEmulator_DebugHelper>("sampleApi")
        .WithReference(keyVault)
        .WaitFor(keyVault);
}
else
{
    builder.AddProject<Projects.AzureKeyVaultEmulator>(AspireConstants.EmulatorServiceName);
}

builder.Build().Run();
