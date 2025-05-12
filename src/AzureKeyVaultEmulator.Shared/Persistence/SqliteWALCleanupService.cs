using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AzureKeyVaultEmulator.Shared.Persistence;
public class SqliteWALCleanupService(IServiceProvider services) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken token)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VaultContext>();

        await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);", token);
    }
}

