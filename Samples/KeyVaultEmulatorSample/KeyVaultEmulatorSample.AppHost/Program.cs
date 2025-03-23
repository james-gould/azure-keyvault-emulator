var builder = DistributedApplication.CreateBuilder(args);

var keyVault = builder.AddAzureKeyVault("keyvault");

builder
    .AddProject<Projects.WebApiWithEmulator>("webapiwithemulator")
    .WithReference(keyVault);

builder.Build().Run();
