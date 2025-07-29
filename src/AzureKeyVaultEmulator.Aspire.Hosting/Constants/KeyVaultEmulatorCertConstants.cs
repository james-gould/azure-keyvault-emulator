namespace AzureKeyVaultEmulator.Aspire.Hosting.Constants;

public sealed class KeyVaultEmulatorCertConstants
{
    private const string _rootName = "emulator";

    public const string HostParentDirectory = "keyvaultemulator";
    public const string HostChildDirectory = "certs";
    public const string HostDotnetDevCertsChildDirectory = "dotnet-dev-certs";

    // PFX is referenced in the Dockerfile, update both is this changes.
    public const string Pfx = $"{_rootName}.pfx";
    public const string Crt = $"{_rootName}.crt";
    public const string Pem = $"{_rootName}.pem";

    // This is also referenced in the Dockerfile, update both if this changes.
    public const string Pword = _rootName;

    public const string CertMountTarget = "/certs";

    public const string Subject = "CN=localhost";

    public const string OSXPath = "/Library/Application Support";
    public const string LinuxPath = "/usr/local/share/ca-certificates";
}
