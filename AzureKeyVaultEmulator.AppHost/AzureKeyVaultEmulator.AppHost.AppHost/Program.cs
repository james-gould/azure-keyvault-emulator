using AzureKeyVaultEmulator.Shared.Constants;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AzureKeyVaultEmulator>(AspireConstants.EmulatorServiceName);

builder.Build().Run();