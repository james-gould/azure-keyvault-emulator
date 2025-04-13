using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AzureKeyVaultEmulator.Shared.Utilities.Attributes
{
    public sealed class SkipTokenAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromQueryMetadata
    {
        public string? Name => "$skiptoken";

        public BindingSource? BindingSource => BindingSource.Query;
    }
}
