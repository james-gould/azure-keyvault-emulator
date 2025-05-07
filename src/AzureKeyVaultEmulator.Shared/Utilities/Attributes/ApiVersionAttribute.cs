using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AzureKeyVaultEmulator.Shared.Utilities.Attributes
{
    public sealed class ApiVersionAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromQueryMetadata
    {
        public string? Name => "api-version";

        public BindingSource? BindingSource => BindingSource.Query;
    }
}
