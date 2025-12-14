using AzureKeyVaultEmulator.Shared.Models;
using AzureKeyVaultEmulator.Shared.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AzureKeyVaultEmulator.Shared.Utilities;

public static class QueryUtils
{
    /// <summary>
    /// Returns a collection of entities representing the latest version for each unique persisted name in the set.
    /// </summary>
    /// <remarks>The latest version is determined by selecting the entity with the latest 'Created'
    /// timestamp in its attributes for each unique persisted name. This method is typically used to identify the
    /// latest state of entities that may have multiple versions.</remarks>
    /// <typeparam name="TEntity">The type of entity contained in the set. Must implement both IAttributedModel{TAttribute} and INamedItem.</typeparam>
    /// <typeparam name="TAttribute">The type of attribute associated with each entity. Must derive from AttributeBase.</typeparam>
    /// <param name="items">The DbSet containing the entities to evaluate. Cannot be null.</param>
    /// <returns>An enumerable collection of entities, each representing the latest version for a given persisted name. If no
    /// entities are present, the collection will be empty.</returns>
    public static IQueryable<TEntity> GetLatestVersions<TEntity, TAttribute>(this DbSet<TEntity> items)
        where TEntity : class, IAttributedModel<TAttribute>, INamedItem
        where TAttribute : AttributeBase
    {
        var minima =
            items
                .Where(x => x.Deleted == false)
                .GroupBy(x => x.PersistedName)
                .Select(g => new
                {
                    Name = g.Key,
                    MaxCreated = g.Max(x => x.Attributes.Created)
                });

        return items
            .Join(
                minima,
                item => new { item.PersistedName, item.Attributes.Updated },
                m => new { PersistedName = m.Name, Updated = m.MaxCreated },
                (item, _) => item
            );
    }
}
