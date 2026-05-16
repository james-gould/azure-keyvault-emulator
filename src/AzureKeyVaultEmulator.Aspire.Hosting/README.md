# Overview

Provides the ability to emulate the `AzureKeyVault` Aspire resource using the open source [emulator](https://github.com/james-gould/azure-keyvault-emulator).

Recommended, but not required, is the [client library](https://www.nuget.org/packages/AzureKeyVaultEmulator.Client) to make using the emulator in your applications incredibly simple.

# Usage

Install the package to your .NET Aspire `AppHost` project:

```
dotnet add package AzureKeyVaultEmulator.Aspire.Hosting
```

Next you can either redirect an existing `AzureKeyVaultResource` to use the emulator, or directly include it without needing any Azure configuration.

To redirect an existing resource:

```csharp
var keyVaultServiceName = "keyvault";

var keyVault = builder
    .AddAzureKeyVault(keyVaultServiceName)
    .RunAsEmulator(); // Add this line

    var webApi = builder
    .AddProject<Projects.MyApi>("api")
    .WithReference(keyvault); // reference as normal
```

To use directly without needing to set up any Azure configuration:

```csharp
var keyVaultServiceName = "keyvault";

var keyVault = builder.AddAzureKeyVaultEmulator(keyVaultServiceName);
```

You will then have a feature complete, emulated `Azure Key Vault` running locally:

![Azure Key Vault Emulator in .NET Aspire](https://i.imgur.com/gMpfwrN.png)

# Using `DefaultAzureCredential`

If your consumer authenticates with `Azure.Identity.DefaultAzureCredential` (so it doesn't need
to depend on the emulator-specific [client library](https://www.nuget.org/packages/AzureKeyVaultEmulator.Client)),
add `WithAzureKeyVaultEmulatorCredentials` in the AppHost. It populates the standard
`AZURE_TENANT_ID` / `AZURE_CLIENT_ID` / `AZURE_CLIENT_SECRET` / `AZURE_AUTHORITY_HOST` env vars
on the consumer pointing at the emulator (preferring `AZURE_TENANT_ID` from the host machine if
it's set), so MSAL can acquire a token locally:

```csharp
// AppHost
var keyVault = builder.AddAzureKeyVaultEmulator("keyvault");

builder.AddProject<Projects.MyApi>("api")
    .WithAzureKeyVaultEmulatorCredentials(keyVault)
    .WithReference(keyVault);
```

In the consumer, wire the Key Vault clients with `DisableInstanceDiscovery = true` and
`DisableChallengeResourceVerification = true` (the emulator runs on `localhost` rather than
`*.vault.azure.net`):

```csharp
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    DisableInstanceDiscovery = true,
});

builder.Services.AddSingleton(_ => new SecretClient(
    new Uri(vaultUri),
    credential,
    new SecretClientOptions { DisableChallengeResourceVerification = true }));
```