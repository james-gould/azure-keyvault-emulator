# Azure KeyVault Emulator TestContainers Module

This module provides TestContainers support for the Azure KeyVault Emulator, enabling easy integration testing with automatic container lifecycle management.

> [!IMPORTANT]
> On `Windows` you will be prompted to install an SSL certificate to the `CurrentUser Trusted Root CA` store on your **first** run.

## Features

- Automatic container lifecycle management
- SSL certificate generation, installation and usage
- Configurable persistence options
- Easy integration with .NET test frameworks

## Basic Usage

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
var secret = await secretClient.GetSecretAsync("mySecretName");
```

## Requirements

- Docker installed and running
- .NET 8.0 or later