# Azure Key Vault Emulator TestContainers Module

This module provides TestContainers support for the Azure KeyVault Emulator, enabling easy integration testing with automatic container lifecycle management.

> [!IMPORTANT]
> On `Windows` you will be prompted to install an SSL certificate to the `CurrentUser Trusted Root CA` store on your **first** run.

## Installation

Simply run the following command in your solution to add the [NuGet Package](https://www.nuget.org/packages/AzureKeyVaultEmulator.TestContainers):

```
dotnet add package AzureKeyVaultEmulator.TestContainers
```

## Features

- Automatic container lifecycle management
- SSL certificate generation, installation and usage
- Configurable persistence options
- Easy integration with .NET test framework
- Full support for CI/CD pipelines (Azure Devops, GitHub Actions, Jekyll etc)

## Requirements

- Docker (or supporting container framework) installed and running
- .NET Standard 2.1 compatible framework (.NET Core 3.1+, .NET 5+)

## SSL Usage

The Azure SDK **requires** a trusted SSL connection to use the official clients. To make this as smooth as possible, the following functionality is turned **on** by default and is **fully automated**:

- Generate the required SSL certificates
- Install them to the `User` store location as a `Trusted Root CA`
    - On `Windows` this will prompt you to confirm the installation. It will only happen on the first run.
- Store the certificates in your host machine's local user area for re-use in subsequent test runs.

The certificates will be stored:

- Windows: `C:/Users/{name}/keyvaultemulator/certs/`
- Unix: `/usr/local/keyvaultemulator/certs/`

If you wish to provide the certificates and disable automatic generation, there are constraints:

- The certificates must be called `emulator.pfx` (and `emulator.crt` if being used on a *NIX host machine)
- The password for `emulator.pfx` **must** be `emulator`.

[See more about configuration here.](#optional-configuration)

## Basic Usage

Using the container can be done without configuration or heavy setup requirements.

```csharp
using AzureKeyVaultEmulator.TestContainers;

// Create container with certificate directory and persistence
await using var container = new AzureKeyVaultEmulatorBuilder().Build();

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

## Optional Configuration

If you wish to alter the default behaviour of the [Azure Key Vault Emulator](https://github.com/james-gould/azure-keyvault-emulator) you can do so with the following:

```csharp
await using var container = new AzureKeyVaultEmulatorBuilder()
    .WithCertificates(
        certificatesDirectory: "my/custom/path/for/ssl/certs",
        generateCertificates: false,
        forceCleanupCertificates: true,
        loadCertificatesIntoTrustStore: false
    )
    .WithPersistence(true)
    .Build();
```

Alternatively you can specify singular options to keep your test code terse:

```csharp
// The configuration constructor
public AzureKeyVaultEmulatorConfiguration(
    string? certificatesDirectory = null,
    bool persist = false,
    bool generateCertificates = true,
    bool forceCleanupCertificates = false,
    bool loadCertificatesIntoTrustStore = true) { ... }

// In your test class
await using var container = new AzureKeyVaultEmulatorContainer(new AzureKeyVaultEmulatorConfiguration(persist: true));
```

[You can find more complete examples in different test frameworks here.](./EXAMPLES.md)
