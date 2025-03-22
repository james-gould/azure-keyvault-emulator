> [!IMPORTANT]
> As of `10/29/2024` [Basis Theory](https://github.com/Basis-Theory/azure-keyvault-emulator) is no longer maintaining the base repository, although the development seemed to be ceased for quite some time prior to that. Due to the ever-growing popularity of `.NET Aspire` the emulator is becoming increasingly more important; I've forked the repository with the goal to create a feature-complete, `.NET Aspire` supported emulator. <br /><br />
> This repo has been detactched from the base repo as of `22/03/2025` due to the change in direction and aspirations for the project., but I cannot thank Basis Theory enough for the original codebase.

## Azure KeyVault Emulator

A fully featured, emulated version of [Azure Key Vault](https://azure.microsoft.com/en-us/products/key-vault) product.

> [!CAUTION]
> This is not a secure space for production secrets, keys or certificates. <br /><br /> Please do not entrust the emulator with real world, high risk items.

# Roadmap

- Introduction of the [full API](https://learn.microsoft.com/en-us/rest/api/keyvault/) for Azure Key Vault:
    - [x] Secrets
    - [ ] Keys
    - [ ] Certificates
    - [ ] Managed HSM
- Separate NuGet package for introducing an emulated Key Vault into your .NET Aspire projects using a Docker Container.
    - This will be an extension of the existing `Aspire.Hosting.Azure.KeyVault` package, downloadable as a separate dependency.

## Supported Operations

### Keys

#### RSA

- Create Key
- Get Key
- Get Key by Version
- Encrypt
- Decrypt
- Supported [Algorithms](https://docs.microsoft.com/en-us/rest/api/keyvault/decrypt/decrypt#jsonwebkeyencryptionalgorithm)
    - `RSA1_5`
    - `RSA-OAEP`

### Secrets

- Set Secret
- Get Secret
- Get Secret by Version
- Delete Secret
- Backup Secret
- Get Secret Versions
- Get Secrets
- Restore Secret
- Update Secret

- Get Deleted Secret
- Get Deleted Secrets
- Purge Deleted Secret
- Recover Deleted Secret

## Requirements

### HTTPS

Azure KeyClient and SecretClient require HTTPS communication with a KeyVault instance.
When accessing the emulator on `localhost`, configure a trusted TLS certificate with [dotnet dev-certs](https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide#with-dotnet-dev-certs).

For accessing the emulator with a hostname other than `localhost`, a self-signed certificate needs to be generated and trusted by the client. See [Adding to docker-compose](#adding-to-docker-compose) for further instructions.

### AuthN/AuthZ

Azure KeyClient and SecretClient use a [ChallengeBasedAuthenticationPolicy](https://github.com/Azure/azure-sdk-for-net/blob/b30fa6d0d402511fdf3270c5d1d9ae5dfa2a0340/sdk/keyvault/Azure.Security.KeyVault.Shared/src/ChallengeBasedAuthenticationPolicy.cs#L64-L66)
to determine the authentication scheme used by the server. In order for the KeyVault Emulator to work with the Azure SDK, the emulator requires JWT authentication in the `Authorization` header with `Bearer` prefix.
KeyVault Emulator only validates the JWT is well-formed.

```shell
curl -X 'GET' \
  'https://localhost:5551/secrets/foo' \
  -H 'accept: application/json' \
  -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE4OTAyMzkwMjIsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0OjUwMDEvIn0.bHLeGTRqjJrmIJbErE-1Azs724E5ibzvrIc-UQL6pws'
```

## Adding to docker-compose

For the Azure KeyVault Emulator to be accessible from other containers in the same compose file, a new OpenSSL certificate has to be generated:
1. Replace `<emulator-hostname>` and run the following script to generate a new public/private keypair:

    ```
    openssl req \
    -x509 \
    -newkey rsa:4096 \
    -sha256 \
    -days 3560 \
    -nodes \
    -keyout <emulator-hostname>.key \
    -out <emulator-hostname>.crt \
    -subj '/CN=<emulator-hostname>' \
    -extensions san \
    -config <( \
      echo '[req]'; \
      echo 'distinguished_name=req'; \
      echo '[san]'; \
      echo 'subjectAltName=DNS.1:localhost,DNS.2:<emulator-hostname>,DNS.3:localhost.vault.azure.net,DNS.4:<emulator-hostname>.vault.azure.net')
    ```

1. Export a `.pks` formatted key using the public/private keypair generated in the previous step:

    ```
    openssl pkcs12 -export -out <emulator-hostname>.pfx \
    -inkey <emulator-hostname>.key \
    -in <emulator-hostname>.crt
    ```

1. Trust the certificate in the login keychain

    ```
    sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain <emulator-hostname>.crt
    ```

1. Add a service to docker-compose.yml for Azure KeyVault Emulator:

    ```
    version: '3.9'
    
    services:
      ...
      azure-keyvault-emulator:
        image: basis-theory/azure-keyvault-emulator:latest
        hostname: <emulator-hostname>.vault.azure.net
        ports:
          - 5001:5001
          - 5000:5000
        volumes:
          - <path-to-certs>:/https
        environment:
          - ASPNETCORE_URLS=https://+:5001;http://+:5000
          - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/<emulator-hostname>.pfx
          - KeyVault__Name=<emulator-hostname>
    ```

1. Modify the client application's entrypoint to add the self-signed certificate to the truststore. Example using docker-compose.yml to override the entrypoint:

    ```
    version: '3.9'
    
    services:
      my-awesome-keyvault-client:
        container_name: my-awesome-client
        build:
          context: .
        depends_on:
          - azure-keyvault-emulator
        entrypoint: sh -c "cp /https/<emulator-hostname>.crt /usr/local/share/ca-certificates/<emulator-hostname>.crt && update-ca-certificates && exec <original-entrypoint>"
        volumes:
          - <path-to-certs>:/https
        environment:
          - KeyVault__BaseUrl=https://<emulator-hostname>.vault.azure.net:5001/
    ```

1. (Optional) Azure KeyVault SDKs verify the challenge resource URL as of v4.4.0 (read more [here](https://devblogs.microsoft.com/azure-sdk/guidance-for-applications-using-the-key-vault-libraries/)). 
To satisfy the new challenge resource verification requirements, do one of the following:
   1. Use an emulator hostname that ends with `.vault.azure.net` (e.g. `localhost.vault.azure.net`). A new entry may need to be added to `/etc/hosts` to properly resolve DNS (i.e. `127.0.0.1 localhost.vault.azure.net`).
   1. Set `DisableChallengeResourceVerification` to true in your client options to disable verification.
```csharp
var client = new SecretClient(
    new Uri("https://localhost.vault.azure.net:5551/"), 
    new LocalTokenCredential(), 
    new SecretClientOptions
    {
        DisableChallengeResourceVerification = true
    });
```

## Development

The provided scripts will check for all dependencies, start docker, build the solution, and run all tests.

### Dependencies
- [Docker](https://www.docker.com/products/docker-desktop)
- [Docker Compose](https://www.docker.com/products/docker-desktop)
- [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### Build the KeyVault emulator and run Tests

Run the following command from the root of the project:

```sh
make verify
```
