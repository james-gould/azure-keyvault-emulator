# Azure Key Vault Emulator

A fully featured, emulated version of [Azure Key Vault](https://azure.microsoft.com/en-us/products/key-vault) product.

`.NET Aspire` has the ability to create emulated, easily referenced resources in development environments - sadly Key Vault is not one of those. To work with Key Vault in a dev-env you need to have a deployed, real world instance of the resource in an active Azure Subscription; this emulator removes that requirement.

The emulator does **not** connect to or update an existing Azure Key Vault, it simply mimics the API (with identical functionality) allowing you to build applications without needing to host a real resource.

# Usage

## Prerequisites

- [Docker](https://www.docker.com/)

## Quickstart (.NET Aspire)

1. Install the [Hosting](https://www.nuget.org/packages/AzureKeyVaultEmulator.Aspire.Hosting) package into your `AppHost` project:

```
dotnet add package AzureKeyVaultEmulator.Aspire.Hosting
```

2. Next you can either override an existing Aspire `AzureKeyVaultResource` or directly include the `AzureKeyVaultEmulator`. 

```csharp

var keyVaultServiceName = "keyvault"; // Remember this string, you'll need it to get the vaultUri!

// With existing resource, requires Azure configuration in your AppHost
var keyVault = builder
    .AddAzureKeyVault(keyVaultServiceName)
    .RunAsEmulator(); // Add this line

// OR directly add the emulator as a resource, no configuration required
var keyVault = builder.AddAzureKeyVaultEmulator(keyVaultServiceName);

var webApi = builder
    .AddProject<Projects.MyApi>("api")
    .WithReference(keyVault); // reference as normal
```

3. Install the [Client](https://www.nuget.org/packages/AzureKeyVaultEmulator.Client) package into your application using Azure Key Vault:

```
dotnet add package AzureKeyVaultEmulator.Client
```

4. Get the connection string that `.NET Aspire` has injected for you and dependency inject the `AzureClients` you need:

```csharp
// Injected by Aspire using the name "keyvault".
var vaultUri = builder.Configuration.GetConnectionString("keyvault") ?? string.Empty;

// Basic Secrets only implementation
builder.Services.AddAzureKeyVaultEmulator(vaultUri);

// Or configure which clients you need to use
builder.Services.AddAzureKeyVaultEmulator(vaultUri, secrets: true, keys: true, certificates: false);
```

5. Now you can use your `AzureClients` as normal dependency injected services:

```csharp
private SecretClient _secretClient;

public SecretsController(SecretClient secretClient)
{
    _secretClient = secretClient;
}

public async Task<string> GetSecretValue(string name)
{
    var secret = await _secretClient.GetSecretAsync(name);

    return secret.Value
}
```

## Optional

Configure your `Program.cs` to optionally inject the emulated or real Azure Key Vault clients depending on your current execution environment:

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

While the primary purpose of this (forked) project is to provide native `.NET Aspire` support it does *not* require it. To use the emulator in a different environment simply pull down the image and follow the [setup instructions](https://github.com/james-gould/azure-keyvault-emulator/blob/development/DOCKER-SETUP.md):

```
docker pull jamesgoulddev/azure-keyvault-emulator:latest
```

# Roadmap

Some API functionality may not be supported while the initial development is ongoing, please refer to the roadmap below to double check if you're attempting a supported operation. The full API *will* be supported, but if you run into issues beforehand that's likely the reason why.

- [ ] Introduction of the [full API](https://learn.microsoft.com/en-us/rest/api/keyvault/) for Azure Key Vault:
    - [x] Secrets
    - [ ] Keys
    - [ ] Certificates
    - [ ] Managed HSM
- [x] Separate NuGet package for introducing an [emulated Key Vault into your .NET Aspire](https://github.com/james-gould/azure-keyvault-emulator/tree/development/AzureKeyVaultEmulator.Hosting.Aspire) projects.
- [x] Separate NuGet package for easy usage of the [emulator in client applications](https://github.com/james-gould/azure-keyvault-emulator/tree/development/AzureKeyVaultEmulator.Client).
- [ ] Complete `docker-compose` options for integrating the emulator into a cluster.
    

## Supported Operations

> [!CAUTION]
> This is not a secure space for production secrets, keys or certificates.


### Keys

#### RSA

- Create Key
- Get Key
- Get Key by Version
- Encrypt
- Decrypt
- Supported [Algorithms](https://learn.microsoft.com/en-us/rest/api/keyvault/keys/decrypt/decrypt?view=rest-keyvault-keys-7.4&tabs=HTTP#jsonwebkeyencryptionalgorithm)
    - `RSA1_5`
    - `RSA-OAEP`

### Secrets

- Set Secret
- Get Secret
- Get Secret by Version
- Delete Secret
- Backup Secret
- Get Secret Versions
- Get Secrets
- Restore Secret
- Update Secret
- Get Deleted Secret
- Get Deleted Secrets
- Purge Deleted Secret
- Recover Deleted Secret
