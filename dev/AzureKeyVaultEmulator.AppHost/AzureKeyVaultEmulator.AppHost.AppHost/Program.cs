using Azure.Security.KeyVault.Keys;
using AzureKeyVaultEmulator.AppHost;
using AzureKeyVaultEmulator.Aspire.Hosting;
using AzureKeyVaultEmulator.Shared.Constants.Orchestration;
using AzureKeyVaultEmulator.Shared.Utilities;

var builder = DistributedApplication.CreateBuilder();

var seedingTestRun = args.GetFlag(AspireConstants.SeedingTest);

if (!seedingTestRun)
{
    var keyVault = builder
    .AddProject<Projects.AzureKeyVaultEmulator>(AspireConstants.EmulatorServiceName);
    //.WithEnvironment("Persist", "true");

    builder.AddProject<Projects.WebApiWithEmulator_DebugHelper>("api")
        .WithReference(keyVault);
}
else
{
    var keyVault = builder
        .AddAzureKeyVault(AspireConstants.EmulatorServiceName)
        .RunAsEmulator()
        .SeedWithSecret("first", "secretValue")
        .SeedWithCertificate("testingCert")
        .SeedWithKey("testingKey", KeyType.Rsa);

    //builder.AddProject<Projects.WebApiWithEmulator_DebugHelper>("api")
    //    .WithReference(keyVault);
}

builder.Build().Run();
