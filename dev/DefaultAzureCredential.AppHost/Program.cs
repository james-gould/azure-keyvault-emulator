using AzureKeyVaultEmulator.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var emulator = builder.AddProject<Projects.AzureKeyVaultEmulator>("keyvault-emulator");

builder.AddProject<Projects.DefaultAzureCredential_DebugWebApi>("debug-api")
    .WithEnvironment("VAULT_URI", emulator.GetEndpoint("https"))
    .WithAzureKeyVaultEmulatorCredentials(emulator)
    .WithReference(emulator)
    .WaitFor(emulator);

builder.Build().Run();

