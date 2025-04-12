using AzureKeyVaultEmulator.Keys.Services;
using AzureKeyVaultEmulator.Secrets.Services;

namespace AzureKeyVaultEmulator.ApiConfiguration
{
    public static class ServiceRegistration
    {
        public static IServiceCollection RegisterCustomServices(this IServiceCollection services)
        {
            services.AddSingleton<IKeyService, KeyService>();
            services.AddSingleton<ISecretService, SercretService>();

            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddTransient<ITokenService, TokenService>();

            return services;
        }
    }
}
