using AzureKeyVaultEmulator.ApiConfiguration;
using AzureKeyVaultEmulator.Middleware;
using AzureKeyVaultEmulator.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddConfiguredAuthentication();

builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddConfiguredSwaggerGen();
builder.Services.RegisterCustomServices();

var app = builder.Build();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
