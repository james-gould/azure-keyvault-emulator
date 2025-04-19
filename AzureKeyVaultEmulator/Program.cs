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

#pragma warning disable ASP0014 // Suggest using top level route registrations
//app.UseEndpoints(e =>
//{
//    e.MapControllerRoute(
//        name: "pendingCertificates",
//        pattern: "{name}/pending",
//        defaults: new { controller = "CertificateOperationsController", action = "GetPendingCertificate" }
//    );

//    e.MapControllerRoute(
//        name: "completedCertificates",
//        pattern: "{name}/completed",
//        defaults: new { controller = "CertificateOperationsController", action = "GetPendingCertificate" }
//    );

//    e.MapControllerRoute(
//        name: "versionedCertificates",
//        pattern: "{name}/{version:regex(^(?!pending$|completed$).+)}",
//        defaults: new { controller = "CertificatesController", action = "GetCertificateByVersion" }
//    );
//});
#pragma warning restore ASP0014 // Suggest using top level route registrations

app.MapControllers();

app.Run();
