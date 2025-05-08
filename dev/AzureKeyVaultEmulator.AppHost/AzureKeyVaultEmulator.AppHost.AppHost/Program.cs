using AzureKeyVaultEmulator.Aspire.Hosting;
using AzureKeyVaultEmulator.Shared.Constants;

var builder = DistributedApplication.CreateBuilder();

// Horrendous bodge for integration testing but doing a full RootCommand pattern here for 1 arg feels... overkill;
var integrationTestRun = args.Where(x => x.Equals("--test")).FirstOrDefault() is not null;
var overrideTestRun = Convert.ToBoolean(Environment.GetEnvironmentVariable("Override") ?? "false");

if (integrationTestRun || overrideTestRun)
{
    builder.AddProject<Projects.AzureKeyVaultEmulator>(AspireConstants.EmulatorServiceName);
}
else
{
    var keyVault = builder
    .AddAzureKeyVault(AspireConstants.EmulatorServiceName)
    .RunAsEmulator(configSectionName: "KeyVaultEmulator");

    var webApi = builder
        .AddProject<Projects.WebApiWithEmulator_DebugHelper>("sampleApi")
        .WaitFor(keyVault);
}

builder.Build().Run();
