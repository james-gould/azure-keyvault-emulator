# Azure Key Vault Emulator

A fully featured, emulated version of the [Azure Key Vault](https://azure.microsoft.com/en-us/products/key-vault) runnable from your development machine.

In a dev environment, currently, you need to have a real Azure Key Vault resource deployed and potentially being paid for in an active Azure subscription. If you’re like me and work for a fairly large company then the security policies around accessing these resources can be tough to navigate, meaning long delays during onboarding and potentially longer delays caused by multiple developers overwriting each other’s secure values.

Microsoft have put significant effort into making the cloud development experience easier with .NET and have released emulators for products that face the same issue. The Azure Service Bus now has an official Emulator to solve that problem for example, sadly Azure Key Vault does not have a similar alternative. Or... did not.

The emulator does not connect to or update an existing Azure Key Vault, it simply mimics the API (with identical functionality) allowing you to build applications without needing to host a real resource.

## Features

- Full Azure SDK Client support; use `SecretClient`, `KeyClient` or `CertificateClient` as normal.
- Destroy all secrets between sessions, or keep a persisted database.
- Works standalone with [Docker](#running-the-emulator-with-docker), easy integration with [.NET Aspire](#running-the-emulator-with-net-aspire).
- [TestContainers Support.](./src/TestContainers/dotnet/)

You can find [sample applications here](https://github.com/james-gould/azure-keyvault-emulator-samples) or you can [read the full launch blog post here!](https://jamesgould.dev/posts/Azure-Key-Vault-Emulator/)

## Prerequisites

- You'll need [Docker](https://www.docker.com/) installed, or [Podman](https://podman.io/) installed and configured to support Docker commands.

## Running the Emulator with Docker

The setup process can be fully automated by using the installation script:

```
bash <(curl -fsSL https://raw.githubusercontent.com/james-gould/azure-keyvault-emulator/refs/heads/master/docs/setup.sh)
```

> [!IMPORTANT]
> If you're using **Windows**, use `Git Bash` to execute the setup script.

Alternatively you can download a copy of [setup.sh](docs/setup.sh) and run it locally, or read the [long form, manual set up docs.](docs/CONFIG.md#local-docker)

The script is interactive and will create the necessary SSL certificates, install them to your `User` trust store and provide the commands to run the container. Once configured, you can start the Emulator with:

```
docker run -d -p 4997:4997 -v {/host/path/to/certs}:/certs -e Persist=true jamesgoulddev/azure-keyvault-emulator:latest
```

A break down of the command:

| Command | Description | Optional? |
| ------- | ----------- | --------- |
| `-d`    | Runs the container in `detatched` mode. | ✅ |
| `-p 4997: 4997`    | Specifies the port to run on. Note `4997` is required, it is **not** configurable currently. | ✅ |
| `-v {/host/path/to/certs}:/certs` | Binds the directory containing the SSL `PFX` and `CRT` files, required for the Azure SDK. | ❌ |
| `-e Persist=true` | Instructs the emulator to create an `SQLite` database, written to your mounted volume/directory alongside the certificate files. | ✅ |
| `jamesgoulddev/azure-keyvault-emulator:latest` | The container image name and tag. Always use `latest`. | ❌ |

You can read more about configuration [here.](docs/CONFIG.md#local-docker)

## Running the Emulator with .NET Aspire

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

## Using The Emulator in your applications.

### 1. Permit requests to the Emulator using the Azure SDK:

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

Or if you don't want to introduce a new dependency you can achieve the same behaviour with `ClientOptions`. 

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

### 2. Use your `AzureClients` as normal dependency injected services:

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

<details>

<summary>Optional, if you're using the AzureKeyVaultEmulator.Client package</summary>

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

</details>

## TestContainers Module

There is a readily available [TestContainers](./src/TestContainers/dotnet/README.md) module too, which fully supports all CI/CD pipelines. 

The same testing setup can be used in your local environment and CI/CD pipelines, no need to set flags or configuration.

### Basic Usage

```csharp
using AzureKeyVaultEmulator.TestContainers;

// Create container with certificate directory and persistence
await using var container = new AzureKeyVaultEmulatorContainer();

// Start the container
await container.StartAsync();

// Get a AzureSDK KeyClient configured for the container
var keyClient = container.GetKeyClient();

// Get a AzureSDK SecretClient configured for the container
var secretClient = container.GetSecretClient();

// Get a AzureSDK CertificateClient configured for the container
var certificateClient = container.GetCertificateClient();

// Use as normal
var secret = await secretClient.SetSecretAsync("mySecretName", "mySecretValue");
```

[You can see more examples here for various test frameworks and scenarios.](./src/TestContainers/dotnet/EXAMPLES.md)

# Roadmap

The Azure Key Vault Emulator is now **stable** and ready for public consumption, however maintenance and enhancement work will continue to ensure the longevity of the project. Below you can find previous and upcoming additions to the project, if you'd like to see something added please raise a [feature request.](https://github.com/james-gould/azure-keyvault-emulator/issues/new?template=feature_request.md)

## Pending

- [ ] Management UI, similar to the Azure Portal UI. (#195)

## Completed

- [x] Introduction of the [full API](https://learn.microsoft.com/en-us/rest/api/keyvault/) for Azure Key Vault:
    - [x] Secrets
    - [x] Keys
    - [x] Certificates
    - [x] Managed HSM
- [x] Separate NuGet package for introducing an [emulated Key Vault into your .NET Aspire](https://github.com/james-gould/azure-keyvault-emulator/tree/development/AzureKeyVaultEmulator.Aspire.Hosting) projects.
- [x] Separate NuGet package for easy usage of the [emulator in client applications](https://github.com/james-gould/azure-keyvault-emulator/tree/development/AzureKeyVaultEmulator.Client).
- [x] Optional vault data persistence and importing for dev environment distribution. (#196)
- [x] Automated environment + Docker setup script, and documentation updated to reflect it.
- [x] TestContainers module. (#158)
