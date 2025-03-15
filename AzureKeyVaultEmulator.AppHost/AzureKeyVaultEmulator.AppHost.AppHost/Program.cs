using AzureKeyVaultEmulator.Shared.Constants;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AzureKeyVaultEmulator>(AspireConstants.EmulatorServiceName);

builder.Build().Run();

public partial class Program { }