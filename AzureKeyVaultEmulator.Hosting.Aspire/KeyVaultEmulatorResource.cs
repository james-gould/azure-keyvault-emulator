﻿using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Hosting.Aspire
{
    public class KeyVaultEmulatorResource 
        : ContainerResource, IResourceWithEndpoints
    {
        public KeyVaultEmulatorResource(AzureKeyVaultResource innerResource) : base(innerResource.Name)
        {
            _innerResource = innerResource;
        }

        private AzureKeyVaultResource _innerResource { get; set; }

        public override ResourceAnnotationCollection Annotations => _innerResource.Annotations;
    }
}
