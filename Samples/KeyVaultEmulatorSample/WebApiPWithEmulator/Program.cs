using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Injected by Aspire
var vaultUri = builder.Configuration.GetConnectionString("keyvault") ?? string.Empty;

// Inject the ones you need, this can be tidied up/abstracted.
// Do NOT use the Aspire KeyVault client library if you need Keys or Certificates, they aren't supported yet.
builder.Services.AddTransient(x => new SecretClient(new Uri(vaultUri), new DefaultAzureCredential()));
builder.Services.AddTransient(x => new KeyClient(new Uri(vaultUri), new DefaultAzureCredential()));
builder.Services.AddTransient(x => new CertificateClient(new Uri(vaultUri), new DefaultAzureCredential()));

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
