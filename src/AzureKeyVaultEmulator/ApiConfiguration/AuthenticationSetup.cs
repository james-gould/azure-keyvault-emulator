using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AzureKeyVaultEmulator.ApiConfiguration
{
    public static class AuthenticationSetup
    {
        private static readonly string? _tenantId = Environment.GetEnvironmentVariable(AuthConstants.TenantIdEnvVar);

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

                        ValidIssuers = [AuthConstants.EmulatorIss],
                        ValidAudiences = [AuthConstants.EmulatorIss],
                        IssuerSigningKey = AuthConstants.SigningKey,
                        SignatureValidator = (token, parameters) => new JsonWebToken(token),
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            var requestHostSplit = context.Request.Host.ToString().Split(".", 2);
                            var scope = $"https://{requestHostSplit[^1]}/.default";

                            var authorization = string.IsNullOrEmpty(_tenantId)
                                ? $"{AuthConstants.EmulatorUri}{context.Request.Path}"
                                : $"{AuthConstants.EmulatorUri}/{_tenantId}";

                            context.Response.Headers.Remove("WWW-Authenticate");
                            context.Response.Headers.WWWAuthenticate = $"Bearer authorization=\"{authorization}\", scope=\"{scope}\", resource=\"https://vault.azure.net\"";

                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();

            return services;
        }
    }
}
