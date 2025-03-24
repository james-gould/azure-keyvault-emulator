using AzureKeyVaultEmulator.Hosting.Aspire;
using AzureKeyVaultEmulator.Shared.Constants;

var builder = DistributedApplication.CreateBuilder(args);

var useEmulatorContainer = bool.Parse(Environment.GetEnvironmentVariable("UseEmulator") ?? string.Empty);

if (useEmulatorContainer)
{
    var keyvault = builder
        .AddAzureKeyVault("keyvault")
        .RunAsEmulator();

    var webApi = builder
        .AddProject<Projects.WebApiWithEmulator>("sampleApi")
        .WithReference(keyvault);

}
else
{
    builder.AddProject<Projects.AzureKeyVaultEmulator>(AspireConstants.EmulatorServiceName);
}

builder.Build().Run();