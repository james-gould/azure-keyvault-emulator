# Azure KeyVault Emulator TestContainers Module

This module provides TestContainers support for the Azure KeyVault Emulator, enabling easy integration testing with automatic container lifecycle management.

## Features

- Automatic container lifecycle management
- SSL certificate handling and validation (also generates certificates automatically)
- Configurable persistence options
- Easy integration with .NET test frameworks

## Usage

```csharp
using AzureKeyVaultEmulator.TestContainers;

// Create container with certificate directory and persistence
var container = new AzureKeyVaultEmulatorContainer("/path/to/certs", persist: true);

// Start the container
await container.StartAsync();

// Use the container endpoint
var endpoint = container.GetConnectionString();

// Clean up
await container.DisposeAsync();
```

## Requirements

- Docker installed and running
- .NET 8.0 or later
- Valid SSL certificates in the specified directory (emulator.pfx required)