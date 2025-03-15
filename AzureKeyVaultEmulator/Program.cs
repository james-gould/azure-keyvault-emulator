using AzureKeyVaultEmulator.ServiceConfiguration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddConfiguredSwaggerGen();
builder.Services.AddConfiguredAuthentication();

builder.Services.RegisterCustomServices();

var app = builder.Build();

app.MapDefaultEndpoints();

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure KeyVault Emulator"));
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.MapGet("/token", () =>
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("cdb1e7890baa2f02708211612646e7b499a44ef5266a7d0ccfbec58e271be316"));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var descriptor = new SecurityTokenDescriptor
    {
        Expires = DateTime.Now.AddDays(31),
        IssuedAt = DateTime.Now,
        NotBefore = DateTime.Now,
        Audience = "azurelocalkeyvault",
        Issuer = "https://localhost",
        SigningCredentials = creds,
    };

    var handler = new JwtSecurityTokenHandler();

    var token = handler.CreateJwtSecurityToken(descriptor);

    return handler.WriteToken(token);
});

app.Run();

// Required for integration testing: https://stackoverflow.com/a/70026704/4664094
public partial class Program { }