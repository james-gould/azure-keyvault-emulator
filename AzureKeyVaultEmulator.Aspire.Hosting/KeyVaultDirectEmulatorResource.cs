using AzureKeyVaultEmulator.Hosting.Aspire;

namespace AzureKeyVaultEmulator.Aspire.Hosting
{
    /// <summary>
    /// Used to create a named container deployment of the emulator without routing through an existing Azure resource.
    /// </summary>
    /// <param name="name"></param>
    public sealed class KeyVaultDirectEmulatorResource(string name)
        : ContainerResource(name), IResourceWithConnectionString
    {
        private EndpointReference _containerEndpoint => new (this, KeyVaultEmulatorContainerImageTags.Name);

        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create(
                $"{_containerEndpoint.Scheme}://{_containerEndpoint.Property(EndpointProperty.Host)}:{_containerEndpoint.Property(EndpointProperty.Port)}");
    }
}
