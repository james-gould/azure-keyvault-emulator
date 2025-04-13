using AzureKeyVaultEmulator.Hosting.Aspire;
using AzureKeyVaultEmulator.Shared.Constants;

var builder = DistributedApplication.CreateBuilder(args);

var useEmulatorContainer = bool.Parse(Environment.GetEnvironmentVariable("UseEmulator") ?? "false");
var keyVaultServiceName = "keyvault";

if (useEmulatorContainer)
{
    //var keyVault = builder
    //    .AddAzureKeyVault(keyVaultServiceName)
    //    .RunAsEmulator();

    var keyVault = builder.AddAzureKeyVaultEmulator(keyVaultServiceName);

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
