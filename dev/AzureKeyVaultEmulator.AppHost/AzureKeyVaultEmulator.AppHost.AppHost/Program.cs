using AzureKeyVaultEmulator.Aspire.Hosting;
using AzureKeyVaultEmulator.Shared.Constants;
using AzureKeyVaultEmulator.Shared.Utilities;

var builder = DistributedApplication.CreateBuilder();

// Horrendous bodge for integration testing but doing a full RootCommand pattern here for 1 arg feels... overkill;
var integrationTestRun = args.Where(x => x.Equals("--test")).FirstOrDefault() is not null;

var overrideTestRun = EnvUtils.GetFlag("Override");
var persist = EnvUtils.GetFlag(EnvironmentConstants.UsePersistedDataStore);

if (integrationTestRun || overrideTestRun)
{
    builder
        .AddProject<Projects.AzureKeyVaultEmulator>(AspireConstants.EmulatorServiceName)
        .WithEnvironment(EnvironmentConstants.UsePersistedDataStore, persist.ToString());
}
else
{
    var keyVault = builder
        .AddAzureKeyVault(AspireConstants.EmulatorServiceName)
        .RunAsEmulator();

    var webApi = builder
        .AddProject<Projects.WebApiWithEmulator_DebugHelper>("sampleApi")
        .WithReference(keyVault)
        .WaitFor(keyVault);
}

builder.Build().Run();
