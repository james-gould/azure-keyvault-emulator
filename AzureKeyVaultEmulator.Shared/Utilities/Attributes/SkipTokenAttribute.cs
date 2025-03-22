using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Shared.Utilities.Attributes
{
    public sealed class SkipTokenAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromQueryMetadata
    {
        public string? Name => "$skiptoken";

        public BindingSource? BindingSource => BindingSource.Query;
    }
}
