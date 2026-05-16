using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AzureKeyVaultEmulator.ApiConfiguration
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

                        ValidIssuers = [AuthConstants.EmulatorIss],
                        ValidAudiences = [AuthConstants.EmulatorIss],
                        IssuerSigningKey = AuthConstants.SigningKey,
                        SignatureValidator = (token, parameters) => new JsonWebToken(token),
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            // Point Azure SDK challenge-based auth at the emulator's own OAuth2 surface (see OAuthController).
                            var tenantId = Environment.GetEnvironmentVariable(AuthConstants.HostMachineTenantId);
                            if (string.IsNullOrWhiteSpace(tenantId))
                                tenantId = AuthConstants.EmulatorTenantId;

                            var authority = $"{context.Request.Scheme}://{context.Request.Host}/{tenantId}";

                            context.Response.Headers.Remove("WWW-Authenticate");
                            context.Response.Headers.WWWAuthenticate =
                                $"Bearer authorization=\"{authority}\", scope=\"https://vault.azure.net/.default\", resource=\"https://vault.azure.net\"";

                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();

            return services;
        }
    }
}
