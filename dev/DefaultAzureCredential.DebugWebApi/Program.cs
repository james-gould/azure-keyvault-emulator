using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;

// Minimal Debug Web API used to exercise the official Azure SDK's DefaultAzureCredential
// against the Azure Key Vault Emulator. By design this project takes NO dependencies on any
// emulator-specific client/wrapper packages — only the official Azure SDK packages.

var builder = WebApplication.CreateBuilder(args);

// The vault URI is supplied to this process as an environment variable by the Aspire AppHost
// orchestrating the test run.
var vaultUri = Environment.GetEnvironmentVariable("VAULT_URI")
    ?? throw new InvalidOperationException(
        "VAULT_URI environment variable was not set. The Aspire AppHost must populate it with the emulator endpoint.");

// DefaultAzureCredential is configured with options that allow it to authenticate against the
// emulator's own OAuth2 surface in test environments. AuthorityHost / AZURE_AUTHORITY_HOST is
// supplied via env-var by the Aspire AppHost (see WithAzureKeyVaultEmulatorCredentials); we also
// disable instance discovery so MSAL does not insist on a known Microsoft cloud, and we
// allow any tenant so the tenant id from the WWW-Authenticate challenge is honoured.
var credentialOptions = new DefaultAzureCredentialOptions
{
    DisableInstanceDiscovery = true,
    //AdditionallyAllowedTenants = { "*" },
};

var credential = new DefaultAzureCredential(credentialOptions);

// SecretClient/KeyClient/CertificateClient verify by default that the WWW-Authenticate "resource"
// returned by the server matches the domain of the request URI. Because the emulator is hosted on
// localhost (not *.vault.azure.net), we must opt out of that check via official SDK options.
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
