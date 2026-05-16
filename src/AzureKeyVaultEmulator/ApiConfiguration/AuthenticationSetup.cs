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
                            // Challenge-based authentication policy in the Azure SDK for .NET parses the
                            // WWW-Authenticate response header from an unauthenticated request to discover
                            //   1. the authority URL it should request a token from
                            //      (the LAST path segment is interpreted as the tenant id), and
                            //   2. the resource / scope the token should be issued for.
                            //
                            // For DefaultAzureCredential to work, the authority URL must therefore be a
                            // valid OAuth2 authority. We point clients at this emulator instance itself
                            // (it exposes a minimal Entra-compatible OAuth2 surface, see OAuthController),
                            // with a tenant id taken from AZURE_TENANT_ID if the host provides one, falling
                            // back to a fixed placeholder GUID otherwise.
                            var tenantId = Environment.GetEnvironmentVariable(AuthConstants.TenantIdEnvironmentVariable);
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
