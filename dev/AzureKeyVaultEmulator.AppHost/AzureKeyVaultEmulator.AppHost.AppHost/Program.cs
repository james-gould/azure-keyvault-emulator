using Azure.Security.KeyVault.Keys;
using AzureKeyVaultEmulator.AppHost;
using AzureKeyVaultEmulator.Aspire.Hosting;
using AzureKeyVaultEmulator.Shared.Constants.Orchestration;
using AzureKeyVaultEmulator.Shared.Utilities;

var builder = DistributedApplication.CreateBuilder();

var seedingTestRun = args.GetFlag(AspireConstants.SeedingTest);
var staticPortTestRun = args.GetFlag(AspireConstants.StaticPortTest);

if (staticPortTestRun)
{
    // Runs the emulator as a container exposed on a fixed host port with persistence enabled.
    // This mirrors the scenario the static-port option fixes: the vault URI (and the URIs embedded
    // within persisted secrets, keys and certificates) stays constant so a subsequent run can reach
    // data created by a prior run.
    builder
        .AddAzureKeyVault(AspireConstants.EmulatorServiceName)
        .RunAsEmulator(new KeyVaultEmulatorOptions
        {
            Port = AspireConstants.StaticPortTestPort,
            Persist = true,
            ImageTag = "latest",
        });
}
else if (!seedingTestRun)
{
    var keyVault = builder
    .AddProject<Projects.AzureKeyVaultEmulator>(AspireConstants.EmulatorServiceName);
    //.WithEnvironment("Persist", "true");

    builder.AddProject<Projects.WebApiWithEmulator_DebugHelper>("api")
        .WithEnvironment($"ConnectionStrings__{AspireConstants.EmulatorServiceName}", keyVault.GetEndpoint("https"));
}
else
{
    var keyVault = builder
        .AddAzureKeyVault(AspireConstants.EmulatorServiceName)
        .RunAsEmulator(new KeyVaultEmulatorOptions { ImageTag = "3.0.0" })
        .SeedWithSecret(SeedingConstants.SeededSecretName, SeedingConstants.SeededSecretValue)
        .SeedWithCertificate(SeedingConstants.SeededCertificateName)
        .SeedWithKey(SeedingConstants.SeededKeyName, KeyType.Rsa);

    //builder.AddProject<Projects.WebApiWithEmulator_DebugHelper>("api")
    //    .WithReference(keyVault);
}

builder.Build().Run();
