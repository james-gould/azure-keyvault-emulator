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

//builder.Services.AddConfiguredSwaggerGen();
builder.Services.RegisterCustomServices();

// Registers the SQLite database, respecting choice around persisted on disk.
builder.Services.AddVaultPersistenceLayer();

var app = builder.Build();

app.RegisterDoubleSlashBodge();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure Key Vault Emulator"));

    app.UseMiddleware<RequestDumpMiddleware>();
}

app.UseHttpsRedirection();
app.UseForwardedHeaders();
app.UseMiddleware<KeyVaultErrorMiddleware>();
app.UseMiddleware<ClientRequestIdMiddleware>();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<VaultContext>();

// Bodge around SQLite table already exists exception
// migration doesnt seem to scaffold CREATE TABLE IF NOT EXISTS
// and throws error on CertificateContacts already existing...
try
{
    var migrations = await db.Database.GetPendingMigrationsAsync();

    if (migrations.Any())
        await db.Database.MigrateAsync();
}
catch { }

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
