using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.Aspire.Client;
using AzureKeyVaultEmulator.Shared.Constants;
using AzureKeyVaultEmulator.Shared.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var vaultUri = builder.Configuration.GetConnectionString(AspireConstants.EmulatorServiceName) ?? "https://localhost:4997/";
//var vaultUri = "http://localhost:4997/";

builder.Services.AddAzureKeyVaultEmulator(vaultUri, secrets: true, certificates: true, keys: true);

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", async () =>
{
    var client = new CertificateClient(new Uri(AuthConstants.EmulatorUri), new DefaultAzureCredential());

    var certName = Guid.NewGuid().Neat();

    var op = await client.StartCreateCertificateAsync(certName, CertificatePolicy.Default);

    await op.WaitForCompletionAsync();

    var cert = await client.GetCertificateAsync(certName);

    return "alive";
});

app.Run();
