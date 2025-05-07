using AzureKeyVaultEmulator.Hosting.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var keyVault = builder
    .AddAzureKeyVault("keyvault")
    // Basic ContainerLifetime.Session support
    .RunAsEmulator();

// Can also be configured manually:

//var keyVault = builder
//    .AddAzureKeyVault("keyvault")
//    .RunAsEmulator(o => o.WithLifetime(ContainerLifetime.Persistent));

builder
    .AddProject<Projects.WebApiWithEmulator>("webapiwithemulator")
    .WithReference(keyVault)
    .WaitFor(keyVault);

builder.Build().Run();