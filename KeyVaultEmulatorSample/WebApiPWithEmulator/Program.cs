using AzureKeyVaultEmulator.Aspire.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Injected by Aspire using the name "keyvault".
// Change this to whatever your "builder.AddAzureKeyVault("nameHere") is!
var vaultUri = builder.Configuration.GetConnectionString("keyvault") ?? string.Empty;

// Basic Secrets only implementation
builder.Services.AddAzureKeyVaultEmulator(vaultUri);

// Alternatively scaffold them individually like so:
//builder.Services.AddAzureKeyVaultEmulator(vaultUri, secrets: true, keys: true, certificates: false);

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () =>
{
    return "alive";
});

app.Run();
