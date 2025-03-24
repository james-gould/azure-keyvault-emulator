using AzureKeyVaultEmulator.Hosting.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

// Redirect existing resource to the emulator:

//var keyVault = builder
//    .AddAzureKeyVault("keyvault")
//    .RunAsEmulator();

// Or directly create it without an existing Azure tenancy
var keyVault = builder.AddAzureKeyVaultEmulator("keyvault");

// Reference as usual!
builder
    .AddProject<Projects.WebApiWithEmulator>("webapiwithemulator")
    .WithReference(keyVault)
    .WaitFor(keyVault);

builder.Build().Run();