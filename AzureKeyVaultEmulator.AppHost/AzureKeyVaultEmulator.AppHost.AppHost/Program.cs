using AzureKeyVaultEmulator.Hosting.Aspire;
using AzureKeyVaultEmulator.Shared.Constants;

var builder = DistributedApplication.CreateBuilder(args);

var useEmulatorContainer = bool.Parse(Environment.GetEnvironmentVariable("UseEmulator") ?? "false");
var keyVaultServiceName = "keyvault";

if (useEmulatorContainer)
{
    var keyvault = builder
        .AddProject<Projects.AzureKeyVaultEmulator>(keyVaultServiceName);

    var webApi = builder
        .AddProject<Projects.WebApiWithEmulator>("sampleApi")
        .WithEnvironment($"ConnectionStrings__{keyVaultServiceName}", keyvault.GetEndpoint("https"));
}
else
{
    builder.AddProject<Projects.AzureKeyVaultEmulator>(AspireConstants.EmulatorServiceName);
}

builder.Build().Run();