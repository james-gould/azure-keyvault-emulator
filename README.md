## Azure Key Vault Emulator

A fully featured, emulated version of [Azure Key Vault](https://azure.microsoft.com/en-us/products/key-vault) product.

`.NET Aspire` has the ability to create emulated, easily referenced resources in development environments - sadly Key Vault is not one of those. To work with Key Vault in a dev-env you need to have a deployed, real world instance of the resource in an active Azure Subscription; this emulator removes that requirement.

Some API functionality may not be supported while the initial development is ongoing, please refer to the roadmap below to double check if you're attempting a supported operation. The full API *will* be supported, but if you run into issues beforehand that's likely the reason why.

# Roadmap

- [ ] Introduction of the [full API](https://learn.microsoft.com/en-us/rest/api/keyvault/) for Azure Key Vault:
    - [x] Secrets
    - [ ] Keys
    - [ ] Certificates
    - [ ] Managed HSM
- [x] Separate NuGet package for introducing an [emulated Key Vault into your .NET Aspire](https://github.com/james-gould/azure-keyvault-emulator/tree/development/AzureKeyVaultEmulator.Hosting.Aspire) projects.
- [x] Separate NuGet package for easy usage of the [emulator in client applications](https://github.com/james-gould/azure-keyvault-emulator/tree/development/AzureKeyVaultEmulator.Client).
- [ ] Complete `docker-compose` options for integrating the emulator into a cluster.
    

## Supported Operations

> [!CAUTION]
> This is not a secure space for production secrets, keys or certificates.

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