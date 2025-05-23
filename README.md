# Azure Key Vault Emulator

<p align="center"><img src="docs/assets/hero.png" height="25%" width="100%"></p>

A fully featured, emulated version of the [Azure Key Vault](https://azure.microsoft.com/en-us/products/key-vault) runnable from your development machine.

In a dev environment, currently, you need to have a real Azure Key Vault resource deployed and potentially being paid for in an active Azure subscription. If you’re like me and work for a fairly large company then the security policies around accessing these resources can be tough to navigate, meaning long delays during onboarding and potentially longer delays caused by multiple developers overwriting each other’s secure values.

Microsoft have put significant effort into making the cloud development experience easier with .NET and have released emulators for products that face the same issue. The Azure Service Bus now has an official Emulator to solve that problem for example, sadly Azure Key Vault does not have a similar alternative. Or... did not.

The emulator does **not** connect to or update an existing Azure Key Vault, it simply mimics the API (with identical functionality) allowing you to build applications without needing to host a real resource.

You can find [sample applications here](https://github.com/james-gould/azure-keyvault-emulator-samples) or you can [read the full launch blog post here!](https://jamesgould.dev/posts/Azure-Key-Vault-Emulator/)

## Prerequisites

- On your first run you will be prompted to install a `localhost` certificate to support the Emulator SSL. 
    - [Not using .NET Aspire, want to learn more or provide your own certificates?](docs/CONFIG.md)
- You'll need either [Docker](https://www.docker.com/) or [Podman](https://podman.io/) installed on your machine.

## Quickstart with .NET Aspire

### 1. Install the [AzureKeyVaultEmulator.Aspire.Hosting](https://www.nuget.org/packages/AzureKeyVaultEmulator.Aspire.Hosting) package into your `AppHost` project:

```
dotnet add package AzureKeyVaultEmulator.Aspire.Hosting
```

### 2. Override an existing Aspire resource or directly include the `AzureKeyVaultEmulator`: 

```csharp
var keyVaultServiceName = "keyvault"; // Remember this string, you'll need it to get the vaultUri!

// With existing resource, requires Azure configuration in your AppHost
var keyVault = builder
    .AddAzureKeyVault(keyVaultServiceName)
    .RunAsEmulator(); // Add this line

// Or directly add the emulator as a resource, no configuration required
var keyVault = builder.AddAzureKeyVaultEmulator(keyVaultServiceName);

var webApi = builder
    .AddProject<Projects.MyApi>("api")
    .WithReference(keyVault); // reference as normal
```

You can also toggle on persisted data, which creates an `emulator.db` loaded at runtime and updated in real-time. 

```csharp
var keyVaultServiceName = "keyvault";

var keyVault = builder
    .AddAzureKeyVault(keyVaultServiceName)
    .RunAsEmulator(new KeyVaultEmulatorOptions { Persist = true }); // Add this option
```

[Read more about configuration here.](docs/CONFIG.md#aspire-config)

### 3. Permit requests to the Emulator using the Azure SDK:

This can be done easily by installing the [AzureKeyVaultEmulator.Client](https://www.nuget.org/packages/AzureKeyVaultEmulator.Client) package:

```
dotnet add package AzureKeyVaultEmulator.Client
```

And then inject your clients:

```csharp
// Injected by Aspire using the name "keyvault".
var vaultUri = builder.Configuration.GetConnectionString("keyvault") ?? string.Empty;

// Basic Secrets only implementation
builder.Services.AddAzureKeyVaultEmulator(vaultUri);

// Or configure which clients you need to use
builder.Services.AddAzureKeyVaultEmulator(vaultUri, secrets: true, keys: true, certificates: false);
```

Or if don't want to introduce a new dependency you can achieve the same behaviour with `ClientOptions`. 

Setting up a `SecretClient` for example:

```cs
// Injected by Aspire using the name "keyvault".
var vaultUri = builder.Configuration.GetConnectionString("keyvault") ?? string.Empty;

// Allows "localhost" to be used instead of "<vault-name>.vault.azure.net" as the vaultUri
var options = new SecretClientOptions { DisableChallengeResourceVerification = true };

// Inject a SecretClient into your DI container which doesn't validate the VaultUri
builder.Services.AddTransient(s => new SecretClient(new Uri(vaultUri), new DefaultAzureCredential(), options));
```

[You can use this code from the client library](src/AzureKeyVaultEmulator.Client/AddEmulatorSupport.cs#L26-L51) replacing `EmulatedCredential` with `DefaultAzureCredential`.

### 4. Use your `AzureClients` as normal dependency injected services:

```csharp
private SecretClient _secretClient;

public SecretsController(SecretClient secretClient)
{
    _secretClient = secretClient;
}

public async Task<string> GetSecretValue(string name)
{
    var secret = await _secretClient.GetSecretAsync(name);

    return secret.Value;
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

The primary purpose of this project is to provide `.NET Aspire` support it does *not* require it. To use the Emulator in a different environment simply pull down the image and follow the [setup instructions](docs/CertificateUtilities/README.md):

```
docker pull jamesgoulddev/azure-keyvault-emulator:latest
```

> [!NOTE]
> `latest` is now stable and the transitional tag `windows` will be retired soon. If you have a dependency relying on `windows` be sure to migrate off before 19/05/2025.

# Roadmap

The Azure Key Vault Emulator is now **stable** and ready for public consumption. Maintenance and enhancement work will continue to ensure the longevity of the project. Below you can find previous and upcoming additions to the project, if you'd like to see something added please raise a [feature request.](https://github.com/james-gould/azure-keyvault-emulator/issues/new?template=feature_request.md)

## Pending

- [ ] TestContainers module. (#158)
- [ ] Optional vault data persistence and importing for dev environment distribution. (#196)
- [ ] Management UI, similar to the Azure Portal UI. (#195)

## Completed

- [x] Introduction of the [full API](https://learn.microsoft.com/en-us/rest/api/keyvault/) for Azure Key Vault:
    - [x] Secrets
    - [x] Keys
    - [x] Certificates
    - [x] Managed HSM
- [x] Separate NuGet package for introducing an [emulated Key Vault into your .NET Aspire](https://github.com/james-gould/azure-keyvault-emulator/tree/development/AzureKeyVaultEmulator.Aspire.Hosting) projects.
- [x] Separate NuGet package for easy usage of the [emulator in client applications](https://github.com/james-gould/azure-keyvault-emulator/tree/development/AzureKeyVaultEmulator.Client).
