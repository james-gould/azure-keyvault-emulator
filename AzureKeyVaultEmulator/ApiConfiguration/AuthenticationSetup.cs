using AzureKeyVaultEmulator.Shared.Constants;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using static System.Net.WebRequestMethods;

namespace AzureKeyVaultEmulator.ServiceConfiguration
{
    public static class AuthenticationSetup
    {
        public static IServiceCollection AddConfiguredAuthentication(this IServiceCollection services)
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var root = "https://localazurekeyvault.localhost:5000/";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        RequireSignedTokens = false,
                        ValidateIssuerSigningKey = false,
                        TryAllIssuerSigningKeys = false,

                        SignatureValidator = (token, parameters) => new JsonWebToken(token),

                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthConstants.IssuerSigningKey)),

                        ValidIssuer = root,
                        ValidAudience = root,
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            var requestHostSplit = context.Request.Host.ToString().Split(".", 2);
                            var scope = $"https://{requestHostSplit[^1]}/.default";
                            context.Response.Headers.Remove("WWW-Authenticate");
                            context.Response.Headers["WWW-Authenticate"] = $"Bearer authorization=\"{root}{context.Request.Path}\", scope=\"{scope}\", resource=\"https://vault.azure.net\"";

                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();

            return services;
        }
    }
}
