using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;

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
                    Description = "JWT Authorization header using the Bearer scheme. Use 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE4OTAyMzkwMjIsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0OjUwMDEvIn0.bHLeGTRqjJrmIJbErE-1Azs724E5ibzvrIc-UQL6pws'",
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
