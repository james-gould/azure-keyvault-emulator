using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.Aspire.Client;
using AzureKeyVaultEmulator.Shared.Constants.Orchestration;
using AzureKeyVaultEmulator.Shared.Utilities;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var vaultUri = builder.Configuration.GetConnectionString(AspireConstants.EmulatorServiceName);

builder.Services.AddAzureKeyVaultEmulator(vaultUri, secrets: true, certificates: true, keys: true);

var wiremock = Environment.GetEnvironmentVariable("wiremock");

if (!string.IsNullOrEmpty(wiremock))
{
    builder.Services.AddHttpClient("test", (sp, client) =>
    {
        client.BaseAddress = new Uri(wiremock);
    });
}

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

app.MapGet("/test", async ([FromServices] IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("test");

        var response = await client.GetAsync("test");

        var content = await response.Content.ReadAsStringAsync();

        return content;
    }
    catch (Exception)
    {
        throw;
    }
});

app.MapGet("/", async ([FromServices] CertificateClient client) =>
{
    var certName = Guid.NewGuid().Neat();

    var op = await client.StartCreateCertificateAsync(certName, CertificatePolicy.Default);

    await op.WaitForCompletionAsync();

    var cert = await client.GetCertificateAsync(certName);

    return certName;
});

app.Run();
