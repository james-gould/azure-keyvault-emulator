using AzureKeyVaultEmulator.ApiConfiguration;
using AzureKeyVaultEmulator.Middleware;
using AzureKeyVaultEmulator.Shared.Middleware;
using AzureKeyVaultEmulator.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddConfiguredAuthentication();

builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddConfiguredSwaggerGen();
builder.Services.RegisterCustomServices();
builder.Services.AddDbContext<VaultContext>();

var app = builder.Build();

#if DEBUG

// Bodge for cleaning up SQLite temp files.
// Remove after schema has been generated correctly and database works.
// Alternatively map into an extension to tuck it away?
AppDomain.CurrentDomain.ProcessExit += async (_, _) =>
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<VaultContext>();
    await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL)");
};

#endif

app.RegisterDoubleSlashBodge();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure KeyVault Emulator"));

    app.UseMiddleware<RequestDumpMiddleware>();
}

app.UseHttpsRedirection();
app.UseForwardedHeaders();
app.UseMiddleware<KeyVaultErrorMiddleware>();
app.UseMiddleware<ClientRequestIdMiddleware>();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<VaultContext>();
await db.Database.EnsureCreatedAsync();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
