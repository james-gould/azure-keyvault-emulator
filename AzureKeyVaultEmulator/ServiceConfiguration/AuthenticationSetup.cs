using Microsoft.AspNetCore.Authentication.BearerToken;

namespace AzureKeyVaultEmulator.ServiceConfiguration
{
    public static class AuthenticationSetup
    {
        public static IServiceCollection AddConfiguredAuthentication(this IServiceCollection services)
        {
            services
                .AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = BearerTokenDefaults.AuthenticationScheme;
                })
                .AddBearerToken(BearerTokenDefaults.AuthenticationScheme, x =>
                {
                    x.BearerTokenExpiration = TimeSpan.FromDays(1);

                    x.Events = new BearerTokenEvents
                    {
                        OnMessageReceived = async (context) => 
                        {
                            context.Principal = new System.Security.Claims.ClaimsPrincipal();
                            context.Success();

                            await Task.CompletedTask;
                        }
                    };
                });

            return services;
        }
    }
}
