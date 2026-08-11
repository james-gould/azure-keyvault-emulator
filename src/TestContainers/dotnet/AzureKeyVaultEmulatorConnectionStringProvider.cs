using DotNet.Testcontainers.Configurations;

namespace AzureKeyVaultEmulator.TestContainers
{
    public class AzureKeyVaultEmulatorConnectionStringProvider : ContainerConnectionStringProvider<
        AzureKeyVaultEmulatorContainer, AzureKeyVaultEmulatorConfiguration>
    {
        protected override string GetHostConnectionString() => Container.GetConnectionString();
    }
}
