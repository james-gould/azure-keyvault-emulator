# Azure Key Vault Emulator

A fully featured, emulated version of the [Azure Key Vault](https://azure.microsoft.com/en-us/products/key-vault) product.

In a dev environment, currently, you need to have a real Azure Key Vault resource deployed and potentially being paid for in an active Azure subscription. If you’re like me and work for a fairly large company then the security policies around accessing these resources can be tough to navigate, meaning long delays during onboarding and potentially longer delays caused by multiple developers overwriting each other’s secure values.

Microsoft have put significant effort into making the cloud development experience easier with .NET and have released emulators for products that face the same issue. The Azure Service Bus now has an official Emulator to solve that problem for example, sadly Azure Key Vault does not have a similar alternative. Or... did not.

The emulator does **not** connect to or update an existing Azure Key Vault, it simply mimics the API (with identical functionality) allowing you to build applications without needing to host a real resource.

You can find a [sample application here](https://github.com/james-gould/azure-keyvault-emulator/tree/master/Samples/KeyVaultEmulatorSample) or you can [read the full launch blog post here!](https://jamesgould.dev/posts/Azure-Key-Vault-Emulator/)

## Prerequisites

- [Docker](https://www.docker.com/)

## Quickstart (.NET Aspire)

1. Install the [Hosting](https://www.nuget.org/packages/AzureKeyVaultEmulator.Aspire.Hosting) package into your `AppHost` project:

```
dotnet add package AzureKeyVaultEmulator.Aspire.Hosting
```

2. Next you can either override an existing Aspire `AzureKeyVaultResource` or directly include the `AzureKeyVaultEmulator`. 

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

3. Optionally install the [Client](https://www.nuget.org/packages/AzureKeyVaultEmulator.Client) package into your application using Azure Key Vault:

```
dotnet add package AzureKeyVaultEmulator.Client
```

4. Get the connection string that `.NET Aspire` has injected for you and dependency inject the `AzureClients` you need:

```csharp
// Injected by Aspire using the name "keyvault".
var vaultUri = builder.Configuration.GetConnectionString("keyvault") ?? string.Empty;

// Basic Secrets only implementation
builder.Services.AddAzureKeyVaultEmulator(vaultUri);

// Or configure which clients you need to use
builder.Services.AddAzureKeyVaultEmulator(vaultUri, secrets: true, keys: true, certificates: false);
```

> [!NOTE]
> The `AzureKeyVaultEmulator.Client` library is not required for the emulator, it simply creates the requested Azure clients  with `DisableChallengeResourceVerification = true`. You can easily skip this dependency by disabling that setting yourself, [or by copying and tweaking this code into your application](https://github.com/james-gould/azure-keyvault-emulator/blob/development/AzureKeyVaultEmulator.Client/AddEmulatorSupport.cs#L26-L51).

5. Now you can use your `AzureClients` as normal dependency injected services:

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

> [!NOTE]
> There's a [pending PR](https://github.com/dotnet/aspire/pull/8408) to add support for the `KeyClient` and `CertificateClient` into the new `Aspire.Azure.Security.Client` package. Support for these 2 clients is expected in `.NET Aspire 9.3`.

While the primary purpose of this (forked) project is to provide native `.NET Aspire` support it does *not* require it. To use the emulator in a different environment simply pull down the image and follow the [setup instructions](https://github.com/james-gould/azure-keyvault-emulator/issues/164):

```
docker pull jamesgoulddev/azure-keyvault-emulator:latest
```

# Roadmap

Some API functionality may not be supported while the initial development is ongoing, please refer to the roadmap below to double check if you're attempting a supported operation. The full API *will* be supported, but if you run into issues beforehand that's likely the reason why.

- [x] Introduction of the [full API](https://learn.microsoft.com/en-us/rest/api/keyvault/) for Azure Key Vault:
    - [x] Secrets
    - [x] Keys
    - [x] Certificates
    - [x] Managed HSM
- [x] Separate NuGet package for introducing an [emulated Key Vault into your .NET Aspire](https://github.com/james-gould/azure-keyvault-emulator/tree/development/AzureKeyVaultEmulator.Aspire.Hosting) projects.
- [x] Separate NuGet package for easy usage of the [emulator in client applications](https://github.com/james-gould/azure-keyvault-emulator/tree/development/AzureKeyVaultEmulator.Client).
- [ ] TestContainers module.
- [ ] Complete `docker-compose` options for integrating the emulator into a cluster.
