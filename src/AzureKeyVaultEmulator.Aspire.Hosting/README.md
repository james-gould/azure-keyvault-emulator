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

If your application authenticates against Key Vault using `Azure.Identity.DefaultAzureCredential`
— and you don't want to take a dependency on the emulator-specific
[client library](https://www.nuget.org/packages/AzureKeyVaultEmulator.Client) — call
`WithAzureKeyVaultEmulatorCredentials` on the consumer resource. This populates the standard Azure
SDK environment variables (`AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`,
`AZURE_AUTHORITY_HOST`) on the consumer pointing at the emulator's own (Entra-compatible) OAuth2
surface so MSAL can acquire a token locally without ever talking to a real Entra tenant:

```csharp
var keyVault = builder.AddAzureKeyVaultEmulator("keyvault");

builder.AddProject<Projects.MyApi>("api")
    .WithAzureKeyVaultEmulatorCredentials(keyVault)
    .WithReference(keyVault);
```

If `AZURE_TENANT_ID` is set on the host machine, its value is preferred so the emulator's
`WWW-Authenticate` challenge advertises your real tenant. The emulator itself never validates inbound
tokens, so whatever MSAL successfully acquires is accepted.

When wiring the Key Vault clients (`SecretClient`, `KeyClient`, `CertificateClient`) by hand in your
consumer project, set `DisableChallengeResourceVerification = true` on the client options — the
emulator runs on `localhost` rather than `*.vault.azure.net`, so the SDK's domain match would
otherwise fail.