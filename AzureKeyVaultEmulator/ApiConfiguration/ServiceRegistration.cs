using AzureKeyVaultEmulator.Emulator.Services;
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
            services.AddSingleton<IKeyVaultKeyService, KeyVaultKeyService>();
            services.AddSingleton<IKeyVaultSecretService, KeyVaultSecretService>();

            services.AddSingleton<IJweEncryptionService, JweEncryptionService>();
            services.AddTransient<ITokenService, TokenService>();

            return services;
        }
    }
}
