using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Hosting.Aspire
{
    public class KeyVaultEmulatorResource(AzureKeyVaultResource innerResource) 
            : ContainerResource(innerResource.Name), IResourceWithConnectionString
    {
        private AzureKeyVaultResource _innerResource => innerResource;

        public override ResourceAnnotationCollection Annotations => _innerResource.Annotations;

        internal EndpointReference EmulatorEndpoint => new(this, KeyVaultEmulatorContainerImageTags.Name);

        public ReferenceExpression ConnectionStringExpression => 
            ReferenceExpression.Create(
                $"{EmulatorEndpoint.Scheme}://{EmulatorEndpoint.Property(EndpointProperty.HostAndPort)}");
    }
}
