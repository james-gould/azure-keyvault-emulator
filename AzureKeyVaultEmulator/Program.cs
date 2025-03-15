using AzureKeyVaultEmulator.ServiceConfiguration;


var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddConfiguredAuthentication();

builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddConfiguredSwaggerGen();
builder.Services.RegisterCustomServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure KeyVault Emulator"));
}

//app.UseAuthentication();
//app.UseAuthorization();

//app.MapGet("/token", () =>
//{
//    var claims = new[]
//    {
//        new Claim(JwtRegisteredClaimNames.Sub, "localuser"),
//        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
//    };

//    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("this is my custom Secret key for authentication"));
//    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//    var token = new JwtSecurityToken(
//        issuer: "https://localazurekeyvault.localhost.com",
//        audience: "https://localazurekeyvault.localhost.com",
//        claims: claims,
//        expires: DateTime.Now.AddMinutes(30),
//        signingCredentials: creds);

//    return new JwtSecurityTokenHandler().WriteToken(token);
//});

app.MapControllers();

app.Run();

// Required for integration testing: https://stackoverflow.com/a/70026704/4664094
public partial class Program { }