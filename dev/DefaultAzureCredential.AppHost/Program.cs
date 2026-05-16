using AzureKeyVaultEmulator.Aspire.Hosting;

// Aspire AppHost for the DefaultAzureCredential test suite. It boots:
//   1. the Azure Key Vault Emulator (as a project, to keep the test cycle fast)
//   2. a Debug Web API that uses ONLY the official Azure SDK packages and
//      authenticates against the emulator via DefaultAzureCredential.
//
// The debug Web API receives the emulator's vault URI via the VAULT_URI environment
// variable and the AZURE_* credential coordinates required by DefaultAzureCredential
// via WithAzureKeyVaultEmulatorCredentials().

var builder = DistributedApplication.CreateBuilder(args);

var emulator = builder.AddProject<Projects.AzureKeyVaultEmulator>("keyvault-emulator");

builder.AddProject<Projects.DefaultAzureCredential_DebugWebApi>("debug-api")
    .WithEnvironment("VAULT_URI", emulator.GetEndpoint("https"))
    .WithAzureKeyVaultEmulatorCredentials(emulator)
    .WithReference(emulator)
    .WaitFor(emulator);

builder.Build().Run();

