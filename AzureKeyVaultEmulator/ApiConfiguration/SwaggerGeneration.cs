using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

namespace AzureKeyVaultEmulator.ServiceConfiguration
{
    public static class SwaggerGeneration
    {
        public static IServiceCollection AddConfiguredSwaggerGen(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Azure KeyVault Emulator", Version = "v1" });
                c.AddSecurityDefinition("JWT", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Description = "JWT Authorization header using the Bearer scheme. Use '/token to generate a token",
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "JWT" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }
    }
}
