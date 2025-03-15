using AzureKeyVaultEmulator.Keys.Services;
using AzureKeyVaultEmulator.Secrets.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AzureKeyVaultEmulator.ServiceConfiguration
{
    public static class ServiceRegistration
    {
        public static IServiceCollection RegisterCustomServices(this IServiceCollection services)
        {
            services.AddScoped<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IKeyVaultKeyService, KeyVaultKeyService>();
            services.AddScoped<IKeyVaultSecretService, KeyVaultSecretService>();

            return services;
        }
    }
}
