namespace AzureKeyVaultEmulator.Shared.Models;

public interface IAttributedModel<TAttributes> where TAttributes : AttributeBase
{
    TAttributes Attributes { get; set; } 
}
