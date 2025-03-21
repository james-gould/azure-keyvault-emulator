using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Shared.Utilities
{
    public sealed class ApiVersionAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromQueryMetadata
    {
        public string? Name => "api-version";

        public BindingSource? BindingSource => BindingSource.Query;
    }
}
