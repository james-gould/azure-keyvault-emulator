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

The hosting integration exports `addAzureKeyVaultEmulator` and `runAsEmulator`
for Aspire's generated TypeScript API. Existing C# calls are unchanged.

To try the integration from source without publishing a package, configure a
TypeScript AppHost's `aspire.config.json` with the assembly name and a relative
path to the hosting project:

```json
{
  "sdk": { "version": "13.6.0" },
  "appHost": {
    "path": "apphost.mts",
    "language": "typescript/nodejs"
  },
  "packages": {
    "Aspire.Hosting.Azure.KeyVault": "13.6.0",
    "AzureKeyVaultEmulator.Aspire.Hosting": "../../src/AzureKeyVaultEmulator.Aspire.Hosting/AzureKeyVaultEmulator.Aspire.Hosting.csproj"
  }
}
```

Run `aspire restore` to build the integration and generate bindings. Never edit
the generated `.aspire/modules` directory.

```typescript
import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();

const keyVault = await builder.addAzureKeyVaultEmulator('keyvault', {
    port: 4997,
    persist: true,
    useDotnetDevCerts: true,
});

// Alternatively, redirect an existing Azure Key Vault resource.
const redirected = await builder.addAzureKeyVault('redirected').runAsEmulator({
    options: { useDotnetDevCerts: true },
    configSectionName: 'emulator',
});

await builder.build().run();
```

`addAzureKeyVaultEmulator` accepts a flat options object. `runAsEmulator` accepts
an options bag containing `options` and the optional `configSectionName`.
Both can be called without options. Omitting options retains the C#
configuration-section lookup; supplying an options object uses that object
and the existing C# defaults for omitted fields.

The exported options are `lifetime`, `port`, `persist`, `localCertificatePath`,
`loadCertificatesIntoTrustStore`, `shouldGenerateCertificates`, and
`useDotnetDevCerts`. Validation and certificate/trust-store behavior are the
same as in C#: persistence requires a fixed port, and disabling certificate
generation requires a directory containing the emulator certificates. These
exports do not extend the credential or seeding helper APIs.

The source-project smoke test in `dev/TypeScript.AppHost` covers both methods,
all configuration fields, and the generated Azure Key Vault resource builder.
With Aspire CLI 13.6.0 installed, validate it from the repository root:

```bash
aspire restore --apphost dev/TypeScript.AppHost/apphost.mts --non-interactive
cd dev/TypeScript.AppHost
npx --no-install tsc --noEmit -p tsconfig.apphost.json
```

To exercise the exports through the TypeScript-to-.NET runtime without
starting containers, use a publish-mode pipeline preview:

```bash
aspire publish --apphost dev/TypeScript.AppHost/apphost.mts --list-steps --non-interactive
```

This only lists pipeline steps; it does not deploy resources or publish a package.

This fixture checks binding generation and TypeScript compatibility; running
the containers additionally requires Docker and valid, trusted certificates
(including the `certificates` directory used by the custom-certificate example).

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