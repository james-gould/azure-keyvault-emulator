using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;

// Debug Web API exercising DefaultAzureCredential against the emulator. Depends only on the
// official Azure SDK packages.

var builder = WebApplication.CreateBuilder(args);

var vaultUri = Environment.GetEnvironmentVariable("VAULT_URI")
    ?? throw new InvalidOperationException(
        "VAULT_URI environment variable was not set. The Aspire AppHost must populate it with the emulator endpoint.");

// DisableInstanceDiscovery so MSAL does not insist on a known Microsoft cloud; AZURE_AUTHORITY_HOST
// is supplied via env-var by the Aspire AppHost (see WithAzureKeyVaultEmulatorCredentials).
var credentialOptions = new DefaultAzureCredentialOptions
{
    DisableInstanceDiscovery = true,
};

var credential = new DefaultAzureCredential(credentialOptions);

// The emulator is on localhost (not *.vault.azure.net), so opt out of challenge-resource domain matching.
var secretOptions = new SecretClientOptions { DisableChallengeResourceVerification = true };
var keyOptions = new KeyClientOptions { DisableChallengeResourceVerification = true };
var certificateOptions = new CertificateClientOptions { DisableChallengeResourceVerification = true };

builder.Services.AddSingleton(_ => new SecretClient(new Uri(vaultUri), credential, secretOptions));
builder.Services.AddSingleton(_ => new KeyClient(new Uri(vaultUri), credential, keyOptions));
builder.Services.AddSingleton(_ => new CertificateClient(new Uri(vaultUri), credential, certificateOptions));

var app = builder.Build();

// --- Secret endpoints ------------------------------------------------------------------
app.MapGet("/secrets/{name}", async (string name, SecretClient client) =>
{
    var response = await client.GetSecretAsync(name);
    return Results.Ok(new { name = response.Value.Name, value = response.Value.Value });
});

app.MapPost("/secrets/{name}", async (string name, string value, SecretClient client) =>
{
    var response = await client.SetSecretAsync(name, value);
    return Results.Ok(new { name = response.Value.Name, value = response.Value.Value });
});

// --- Key endpoints ---------------------------------------------------------------------
app.MapPost("/keys/{name}", async (string name, KeyClient client) =>
{
    var response = await client.CreateRsaKeyAsync(new CreateRsaKeyOptions(name));
    return Results.Ok(new { name = response.Value.Name, kid = response.Value.Id.ToString() });
});

app.MapGet("/keys/{name}", async (string name, KeyClient client) =>
{
    var response = await client.GetKeyAsync(name);
    return Results.Ok(new { name = response.Value.Name, kid = response.Value.Id.ToString() });
});

// --- Certificate endpoints -------------------------------------------------------------
app.MapPost("/certificates/{name}", async (string name, CertificateClient client) =>
{
    var op = await client.StartCreateCertificateAsync(name, CertificatePolicy.Default);
    await op.WaitForCompletionAsync();
    var cert = await client.GetCertificateAsync(name);
    return Results.Ok(new { name = cert.Value.Name });
});

app.MapGet("/certificates/{name}", async (string name, CertificateClient client) =>
{
    var cert = await client.GetCertificateAsync(name);
    return Results.Ok(new { name = cert.Value.Name });
});

app.Run();

// Exposed so DistributedApplicationTestingBuilder can find this assembly.
public partial class Program;
