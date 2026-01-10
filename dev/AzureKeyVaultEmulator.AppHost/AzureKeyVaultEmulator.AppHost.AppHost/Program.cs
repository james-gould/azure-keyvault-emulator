//using AzureKeyVaultEmulator.Aspire.Hosting;
using AzureKeyVaultEmulator.Shared.Constants.Orchestration;

var builder = DistributedApplication.CreateBuilder();

var keyVault = builder.AddProject<Projects.AzureKeyVaultEmulator>(AspireConstants.EmulatorServiceName);

//var keyVault = builder
//    .AddAzureKeyVault(AspireConstants.EmulatorServiceName)
//    .RunAsEmulator();

var api = builder.AddProject<Projects.WebApiWithEmulator_DebugHelper>("api")
    .WithReference(keyVault);

builder.Build().Run();
