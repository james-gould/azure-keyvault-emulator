# Overview

This library simplifies the inclusion of the Azure Key Vault Emulator into your local development environment.

You do not *need* to use it but it makes life easier. Due to the constraints of Azure Key Vault and the associated client libraries, any requests that don't come from `https://*.vault.azure.net` are rejected.

To work around this you need to set `DisableChallengeResourceVerification = true` for each client. This library does that for you, emulates the authentication and then dependency injects the clients you need.

# Setup

First install the package to your application that needs to use Key Vault:

```
dotnet add package AzureKeyVaultEmulator.Client
```

Next you'll need to reference the `vaultUri` which points at the docker container.

If you're using `.NET Aspire` this will appear in your `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "myAspireResourceName": "<connectionstring will be populated here>"
  }
}

```

> [!NOTE]
> You don't need to add this into your `appsettings.json` beforehand, Aspire will do this for you.


If you're simply running the container locally and directly referencing it, you can find the port for `https://localhost:{port}` with the following:

```
docker ps
```

You'll then need to create a configuration item in your application which allows the dotnet runtime to get the `https://localhost:{port}`.

# Usage

Including support for the emulator is simple:

```csharp
// Injected by Aspire using the name "keyvault".
var vaultUri = builder.Configuration.GetConnectionString("keyvault") ?? string.Empty;

// Basic Secrets only implementation
builder.Services.AddAzureKeyVaultEmulator(vaultUri);
```

You can configure which `Clients` you want to expose like so:

```csharp
builder.Services.AddAzureKeyVaultEmulator(vaultUri, secrets: true, keys: true, certificates: false);
```

By default only a `SecretClient` will be available, but you can easily add `CertificateClient` and `KeyClient` support.

# Access

Now you've got your clients set up you can simply use them like you would any other DI service:

```csharp
private SecretClient _secretClient;

public SecretsController(SecretClient secretClient)
{
    _secretClient = secretClient;
}

public async Task GetSecret(string name)
{
    var secret = await _secretClient.GetSecretAsync(name);
}
```

# Quick Tip

To make life easy you can create an environment flag in your `Program.cs` to use the Emulator in a dev environment and the hosted Vault in production:

```csharp
var vaultUri = builder.Configuration.GetConnectionString("keyvault") ?? string.Empty;

if(builder.Environment.IsDevelopment())
    builder.Services.AddAzureKeyVaultEmulator(vaultUri, secrets: true, certificates: true, keys: true);
else
    builder.Services.AddAzureClients(client =>
    {
        var asUri = new Uri(vaultUri);

        client.AddSecretClient(asUri);
        client.AddKeyClient(asUri);
        client.AddCertificateClient(asUri);
    });
```
