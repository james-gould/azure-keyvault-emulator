using System.Security.Cryptography.X509Certificates;
using Aspire.Hosting.Azure;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.Aspire.Hosting.Helpers;

namespace AzureKeyVaultEmulator.Aspire.Hosting;

public static partial class KeyVaultEmulatorExtensions
{
    private static readonly List<KeyVaultSecret> _seedingSecrets = [];

    // New, blank keys
    private static readonly List<(string keyName, CreateKeyOptions options, KeyType type)> _seedingKeys = [];

    // Existing keys being imported
    private static readonly List<ImportKeyOptions> _seedingExistingKeys = [];

    // New, blank x509 certs
    private static readonly List<(string certificateName, CertificatePolicy policy)> _seedingCertificates = [];

    // existing certs from bytes
    private static readonly List<ImportCertificateOptions> _seedingExistingCertificates = [];

    /// <summary>
    /// Adds a secret to be seeded into the specified Azure Key Vault Emulator resource at runtime.
    /// </summary>
    /// <remarks>This method registers the secret for seeding but does not immediately create it in
    /// Azure Key Vault. The secret will be created when the container is running in a healthy state.</remarks>
    /// <param name="keyVault">The resource builder for the Azure Key Vault to which the secret will be added. Cannot be null.</param>
    /// <param name="secretName">The name of the secret to add. Cannot be null.</param>
    /// <param name="secretValue">The value of the secret to add. Cannot be null.</param>
    /// <returns>The same resource builder instance for the Azure Key Vault, enabling method chaining.</returns>
    public static IResourceBuilder<AzureKeyVaultResource> SeedWithSecret(
        this IResourceBuilder<AzureKeyVaultResource> keyVault, string secretName, string secretValue)
    {
        ArgumentNullException.ThrowIfNull(keyVault);
        ArgumentNullException.ThrowIfNull(secretName);
        ArgumentNullException.ThrowIfNull(secretValue);

        var secret = new KeyVaultSecret(secretName, secretValue);

        _seedingSecrets.Add(secret);

        return keyVault;
    }

    /// <summary>
    /// Adds a key to be seeded into the specified Azure Key Vault Emulator resource at runtime.
    /// </summary>
    /// <remarks>This method registers the specified key to be created in the Key Vault when the
    /// resource is provisioned. It does not immediately create the key in Azure Key Vault; the key will be created
    /// when the container is running in a healthy state.</remarks>
    /// <param name="keyVault">The resource builder for the Azure Key Vault to which the key will be added. Cannot be null.</param>
    /// <param name="keyName">The name of the key to seed into the Key Vault. Cannot be null.</param>
    /// <param name="options">The options used to configure the creation of the key.</param>
    /// <param name="keyType">The type of key to create in the Key Vault.</param>
    /// <returns>The same resource builder instance for the Azure Key Vault, enabling method chaining.</returns>
    public static IResourceBuilder<AzureKeyVaultResource> SeedWithKey(
        this IResourceBuilder<AzureKeyVaultResource> keyVault, string keyName,
        KeyType keyType, CreateKeyOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(keyVault);
        ArgumentException.ThrowIfNullOrEmpty(keyName);

        options ??= new CreateKeyOptions();

        _seedingKeys.Add((keyName, options, keyType));

        return keyVault;
    }

    /// <summary>
    /// Adds a key to be seeded into the specified Azure Key Vault Emulator resource at runtime.
    /// </summary>
    /// <remarks>This method registers the specified key to be created in the Key Vault when the
    /// resource is provisioned. It does not immediately create the key in Azure Key Vault; the key will be created
    /// when the container is running in a healthy state.</remarks>
    /// <param name="keyVault">The resource builder for the Azure Key Vault to which the key will be added. Cannot be null.</param>
    /// <param name="keyName">The name of the key to seed into the Key Vault. Cannot be null.</param>
    /// <param name="jsonWebKey">The key to import.</param>
    /// <param name="keyType">The type of key to create in the Key Vault.</param>
    /// <returns>The same resource builder instance for the Azure Key Vault, enabling method chaining.</returns>
    public static IResourceBuilder<AzureKeyVaultResource> SeedWithKey(
        this IResourceBuilder<AzureKeyVaultResource> keyVault, string keyName,
        JsonWebKey jsonWebKey, KeyType keyType)
    {
        ArgumentNullException.ThrowIfNull(keyVault);
        ArgumentException.ThrowIfNullOrEmpty(keyName);

        var options = new ImportKeyOptions(keyName, jsonWebKey);

        _seedingExistingKeys.Add(options);

        return keyVault;
    }

    /// <summary>
    /// Adds a certificate to be seeded into the specified Azure Key Vault resource at runtime.
    /// </summary>
    /// <remarks>This method is typically used as part of a resource definition chain to ensure that
    /// the specified certificate is created in the Key Vault when the container is running in a healthy state.</remarks>
    /// <param name="keyVault">The resource builder for the Azure Key Vault to which the certificate will be added. Cannot be null.</param>
    /// <param name="certificateName">The name of the certificate to seed. Cannot be null.</param>
    /// <param name="policy">An optional certificate policy to apply when creating the certificate. If null, the default policy is used.</param>
    /// <returns>The original resource builder for the Azure Key Vault, enabling method chaining.</returns>
    public static IResourceBuilder<AzureKeyVaultResource> SeedWithCertificate(
        this IResourceBuilder<AzureKeyVaultResource> keyVault, string certificateName, CertificatePolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(keyVault);
        ArgumentNullException.ThrowIfNull(certificateName);

        _seedingCertificates.Add((certificateName, policy ?? CertificatePolicy.Default));

        return keyVault;
    }

    /// <summary>
    /// Adds a certificate to the Azure Key Vault resource for seeding during initialization.
    /// </summary>
    /// <remarks>Use this method to pre-populate the emulator's key vault with a certificate before starting
    /// the resource. This is useful for integration testing or local development scenarios where a known certificate is
    /// required.</remarks>
    /// <param name="keyVault">The resource builder for the Azure Key Vault to which the certificate will be added.</param>
    /// <param name="certificateName">The name to assign to the certificate within the key vault. Cannot be null or empty.</param>
    /// <param name="certificateBytes">The byte array containing the certificate data to import. Must not be null or empty.</param>
    /// <param name="policy">An optional certificate policy to associate with the imported certificate. If null, the default policy is used.</param>
    /// <returns>The same resource builder instance, enabling method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="certificateName"/> is null or empty, or if <paramref name="certificateBytes"/> is null
    /// or empty.</exception>
    public static IResourceBuilder<AzureKeyVaultResource> SeedWithCertificate(
        this IResourceBuilder<AzureKeyVaultResource> keyVault, string certificateName,
        byte[] certificateBytes, CertificatePolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(keyVault);
        ArgumentException.ThrowIfNullOrEmpty(certificateName);

        if (certificateBytes is null || certificateBytes.Length == 0)
            throw new ArgumentException($"Cannot seed with empty certificate, byte array was null or empty.");

        var importOptions = new ImportCertificateOptions(certificateName, certificateBytes)
        {
            Policy = policy ?? CertificatePolicy.Default
        };

        _seedingExistingCertificates.Add(importOptions);

        return keyVault;
    }

    /// <summary>
    /// Adds a certificate to the Azure Key Vault emulator resource for seeding during initialization.
    /// </summary>
    /// <remarks>Use this method to pre-populate the emulator with a certificate, which can be useful for
    /// integration testing or local development scenarios. The certificate is loaded from the specified file path and
    /// made available in the emulator under the provided name.</remarks>
    /// <param name="keyVault">The resource builder for the Azure Key Vault emulator to which the certificate will be added.</param>
    /// <param name="certificateName">The name to assign to the certificate within the key vault. Cannot be null or empty.</param>
    /// <param name="certificatePath">The file system path to the certificate file to import. Cannot be null or empty.</param>
    /// <param name="policy">An optional certificate policy to associate with the imported certificate. If null, the default policy is used.</param>
    /// <returns>The same resource builder instance, enabling method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if either <paramref name="certificateName"/> or <paramref name="certificatePath"/> is null or empty, or
    /// if the certificate file cannot be found at the specified path.</exception>
    public static IResourceBuilder<AzureKeyVaultResource> SeedWithCertificate(
        this IResourceBuilder<AzureKeyVaultResource> keyVault, string certificateName,
        string certificatePath, CertificatePolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(keyVault);
        ArgumentException.ThrowIfNullOrEmpty(certificateName);
        ArgumentException.ThrowIfNullOrEmpty(certificatePath);

        var exists = File.Exists(certificatePath);

        if (!exists)
            throw new ArgumentException($"Cannot find certificate at provided path: {certificatePath}");

        X509Certificate2 cert = null!;

#if NET8_0
        cert = new X509Certificate2(certificatePath);
#elif NET9_0_OR_GREATER
        cert = X509CertificateLoader.LoadCertificateFromFile(certificatePath);
#endif

        var bytes = cert.RawData;

        var options = new ImportCertificateOptions(certificateName, bytes);

        _seedingExistingCertificates.Add(options);

        return keyVault;
    }

    internal static async ValueTask SeedSecretsFromApphostAsync(string vaultUri, CancellationToken ct)
    {
        if (_seedingSecrets.Count == 0)
            return;

        var client = AzureKeyVaultEmulatorClientHelper.GetSecretClient(vaultUri);

        foreach (var secret in _seedingSecrets)
            await client.SetSecretAsync(secret, ct);
    }

    internal static async ValueTask SeedCertificatesFromAppHostAsync(string vaultUri, CancellationToken ct)
    {
        if (_seedingCertificates.Count == 0 || _seedingExistingCertificates.Count == 0)
            return;

        var client = AzureKeyVaultEmulatorClientHelper.GetCertificateClient(vaultUri);

        foreach(var (certificateName, policy) in _seedingCertificates)
            await client.StartCreateCertificateAsync(certificateName, policy, cancellationToken: ct);

        foreach (var importOptions in _seedingExistingCertificates)
            await client.ImportCertificateAsync(importOptions, ct);
    }

    internal static async ValueTask SeedKeysFromAppHostAsync(string vaultUri, CancellationToken ct)
    {
        if (_seedingKeys.Count == 0 || _seedingExistingKeys.Count == 0)
            return;

        var client = AzureKeyVaultEmulatorClientHelper.GetKeyClient(vaultUri);

        foreach (var (keyName, options, type) in _seedingKeys)
            await client.CreateKeyAsync(keyName, type, options, ct);

        foreach (var options in _seedingExistingKeys)
            await client.ImportKeyAsync(options, ct);
    }
}
