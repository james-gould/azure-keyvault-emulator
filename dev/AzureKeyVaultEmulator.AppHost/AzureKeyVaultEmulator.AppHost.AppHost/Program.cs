using AzureKeyVaultEmulator.Shared.Constants.Orchestration;

var builder = DistributedApplication.CreateBuilder();

var keyVault = builder.AddProject<Projects.AzureKeyVaultEmulator>(AspireConstants.EmulatorServiceName);

builder.Build().Run();
