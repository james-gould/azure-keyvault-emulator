using AzureKeyVaultEmulator.Certificates.Services;
using AzureKeyVaultEmulator.Keys.Services;
using AzureKeyVaultEmulator.Secrets.Services;

namespace AzureKeyVaultEmulator.ApiConfiguration
{
    public static class ServiceRegistration
    {
        public static IServiceCollection RegisterCustomServices(this IServiceCollection services)
        {
            services.AddSingleton<IKeyService, KeyService>();
            services.AddSingleton<ISecretService, SecretService>();

            services.AddSingleton<ICertificateBackingService, CertificateBackingService>();
            services.AddSingleton<ICertificateService, CertificateService>();

            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddTransient<ITokenService, TokenService>();

            return services;
        }
    }
}
