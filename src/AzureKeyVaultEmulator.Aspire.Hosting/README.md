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

# TypeScript AppHosts

Add `AzureKeyVaultEmulator.Aspire.Hosting` to your TypeScript AppHost's integration
packages and run `aspire restore` to generate the bindings.

To use the emulator directly:

```typescript
import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();

const keyVault = await builder.addAzureKeyVaultEmulator('keyvault', {
    port: 4997,
    persist: true,
});

await builder.build().run();
```

To redirect an existing Azure Key Vault resource:

```typescript
const keyVault = await builder.addAzureKeyVault('keyvault').runAsEmulator({
    options: { port: 4997, persist: true },
});
```

Both methods can be called without options. `runAsEmulator` also accepts
`configSectionName` alongside `options`. Omitting the options object retains
configuration-section lookup; supplying it uses the existing C# defaults for
omitted fields. Certificate setup and validation are unchanged, including the
requirement for a fixed `port` when `persist` is enabled.

# Using `DefaultAzureCredential`

If your consumer authenticates with `Azure.Identity.DefaultAzureCredential` (so it doesn't need
to depend on the emulator-specific [client library](https://www.nuget.org/packages/AzureKeyVaultEmulator.Client)),
add `WithAzureKeyVaultEmulatorCredentials` in the AppHost. Everything is wired up automatically —
credentials, tenant, and authority all flow into the consumer for you (the host machine's
`AZURE_TENANT_ID` is picked up automatically when set):

```csharp
// AppHost
var keyVault = builder.AddAzureKeyVaultEmulator("keyvault");

builder.AddProject<Projects.MyApi>("api")
    .WithAzureKeyVaultEmulatorCredentials(keyVault)
    .WithReference(keyVault);
```

The only consumer-side settings the SDK still requires are `DisableInstanceDiscovery = true` and
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