﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AzureKeyVaultEmulator.ServiceConfiguration
{
    public static class AuthenticationSetup
    {
        /// <summary>
        /// We just want to force requests through, the client libraries expect auth but we don't care about it here.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddConfiguredAuthentication(this IServiceCollection services)
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        ValidateIssuerSigningKey = false,

                        SignatureValidator = (token, parameters) => new JsonWebToken(token),
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            var requestHostSplit = context.Request.Host.ToString().Split(".", 2);
                            var scope = $"https://{requestHostSplit[^1]}/.default";
                            context.Response.Headers.Remove("WWW-Authenticate");
                            context.Response.Headers["WWW-Authenticate"] = $"Bearer authorization=\"https://azure-keyvault-emulator.vault.azure.net/{context.Request.Path}\", scope=\"{scope}\", resource=\"https://vault.azure.net\"";

                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();

            return services;
        }
    }
}
