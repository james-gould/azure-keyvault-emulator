using Aspire.Hosting.Azure;

namespace AzureKeyVaultEmulator.Aspire.Hosting
{
    public class KeyVaultEmulatorResource(AzureKeyVaultResource innerResource) 
            : ContainerResource(innerResource.Name), IResourceWithConnectionString
    {
        private AzureKeyVaultResource _innerResource => innerResource;

        public override ResourceAnnotationCollection Annotations => _innerResource.Annotations;

        internal EndpointReference EmulatorEndpoint => new(this, KeyVaultEmulatorContainerImageTags.Name);

        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create(
                $"{EmulatorEndpoint.Scheme}://{EmulatorEndpoint.Property(EndpointProperty.Host)}:{EmulatorEndpoint.Property(EndpointProperty.Port)}");
    }
}
