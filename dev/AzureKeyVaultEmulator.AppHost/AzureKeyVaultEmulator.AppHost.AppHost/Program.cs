using AzureKeyVaultEmulator.Aspire.Hosting;
using AzureKeyVaultEmulator.Shared.Constants;

var builder = DistributedApplication.CreateBuilder();

var keyVault = builder
    .AddAzureKeyVault(AspireConstants.EmulatorServiceName)
    .RunAsEmulator(
    //new KeyVaultEmulatorOptions { Lifetime = ContainerLifetime.Persistent }
    );

var webApi = builder
    .AddProject<Projects.WebApiWithEmulator_DebugHelper>("sampleApi")
    .WithReference(keyVault)
    .WaitFor(keyVault);

builder.Build().Run();
