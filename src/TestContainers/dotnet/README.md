# Azure KeyVault Emulator TestContainers Module

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
- .NET 8.0 or later

## SSL Usage

The Azure SDK **requires** a trusted SSL connection to use the official clients. To make this as smooth as possible, the following functionality is turned **on** by default:

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

## Optional Configuration

If you wish to alter the default behaviour of the [Azure Key Vault Emulator](https://github.com/james-gould/azure-keyvault-emulator) you can do so with the following:

```csharp
public sealed class AzureKeyVaultEmulatorOptions
{
    /// <summary>
    /// Allows the Emulator to persist data beyond temporary storage for multi-session use.
    /// </summary>
    public bool Persist { get; set; } = false;

    /// <summary>
    /// <para>Specify the directory to be used as a mount for the Azure Key Vault Emulator.</para>
    /// <para>Warning: your container runtime must have read access to this directory.</para>
    /// </summary>
    public string LocalCertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// <para>Determines if the Emulator should attempt to load the certificates into the host machine's trust store.</para>
    /// <para>Warning: this requires Administration rights.</para>
    /// <para>Unused if the certificates are already present, removing the administration privilege requirement.</para>
    /// </summary>
    public bool LoadCertificatesIntoTrustStore { get; set; } = true;

    /// <summary>
    /// <para>Disables the Azure Key Vault Emulator creating a self signed SSL certificate for you at runtime.</para>
    /// <para>
    /// Using this option will require you to provide a certificate in PFX (and optionally a CRT) format within the same directory.
    /// The directory must also be set via <see cref="LocalCertificatePath"/>.
    /// </para>
    /// <para>The PFX password MUST be "emulator" - all lowercase without the double quotes. This limitation is being looked into.</para>
    /// </summary>
    public bool ShouldGenerateCertificates { get; set; } = true;

    /// <summary>
    /// <para>Cleans up the generated SSL certificates on application shutdown.</para>
    /// <para>If you do not set a value for <see cref="LocalCertificatePath"/>, the default local user directory will be used for your OS.</para>
    /// <para>Default: <see langword="false"/></para>
    /// </summary>
    public bool ForceCleanupOnShutdown { get; set; } = false;
}

// In your test class

var options = new AzureKeyVaultEmulatorOptions { LocalCertificatePath = "my/custom/path/for/ssl/certs" };

await using var container = new AzureKeyVaultEmulatorContainer(options);
```

Alternatively you can specify singluar options to keep your test code terse:

```csharp
// The container constructor
public AzureKeyVaultEmulatorContainer(
    string? certificatesDirectory = null,
    bool persist = false,
    bool generateCertificates = true,
    bool forceCleanupCertificates = false) { ... }

// In your test class
await using var container = new AzureKeyVaultEmulatorContainer(persist: true);
```

[You can find more complete examples in different test frameworks here.](./EXAMPLES.md)
