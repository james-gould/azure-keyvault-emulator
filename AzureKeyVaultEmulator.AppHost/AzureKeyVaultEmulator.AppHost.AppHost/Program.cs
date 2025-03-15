using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<AzureKeyVaultEmulator>("KeyVaultEmulatorAPI");

builder.Build().Run();