using AzureKeyVaultEmulator.Hosting.Aspire;
using AzureKeyVaultEmulator.Shared.Constants;

var builder = DistributedApplication.CreateBuilder(args);

//var useEmulatorContainer = bool.Parse(Environment.GetEnvironmentVariable("UseEmulator") ?? "false");
var useDeployedDockerContainer = false;

if (useDeployedDockerContainer)
{
    //var keyVault = builder
    //    .AddAzureKeyVault(keyVaultServiceName)
    //    .RunAsEmulator();

    var keyVault = builder.AddAzureKeyVaultEmulator(AspireConstants.EmulatorServiceName);

    var webApi = builder
        .AddProject<Projects.WebApiWithEmulator_DebugHelper>("sampleApi")
        .WithReference(keyVault)
        .WaitFor(keyVault);
}
else
{
    var keyVault = builder.AddProject<Projects.AzureKeyVaultEmulator>(AspireConstants.EmulatorServiceName);

builder
    .AddProject<Projects.WebApiWithEmulator_DebugHelper>("sampleApi")
    .WithReference(keyVault)
    .WaitFor(keyVault);
}

builder.Build().Run();
