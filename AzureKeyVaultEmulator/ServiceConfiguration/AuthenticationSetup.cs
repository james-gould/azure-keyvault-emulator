using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.ServiceConfiguration
{
    public static class AuthenticationSetup
    {
        public static IServiceCollection AddConfiguredAuthentication(this IServiceCollection services)
        {
            services
                .AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, x =>
                {
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = "https://localhost:5001/",
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        RequireSignedTokens = false,
                        ValidateIssuerSigningKey = false,
                        TryAllIssuerSigningKeys = false,
                        SignatureValidator = (token, _) => new JwtSecurityToken(token)
                    };

                    x.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            var requestHostSplit = context.Request.Host.ToString().Split(".", 2);
                            var scope = $"https://{requestHostSplit[^1]}/.default";
                            context.Response.Headers.Remove("WWW-Authenticate");
                            context.Response.Headers["WWW-Authenticate"] = $"Bearer authorization=\"https://localhost:5001/foo/bar\", scope=\"{scope}\", resource=\"https://vault.azure.net\"";
                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }
    }
}
