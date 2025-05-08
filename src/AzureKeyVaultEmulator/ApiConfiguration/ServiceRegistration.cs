using AzureKeyVaultEmulator.Certificates.Services;
using AzureKeyVaultEmulator.Keys.Services;
using AzureKeyVaultEmulator.Secrets.Services;

namespace AzureKeyVaultEmulator.ApiConfiguration
{
    public static class ServiceRegistration
    {
        public static IServiceCollection RegisterCustomServices(this IServiceCollection services)
        {
            services.AddTransient<IKeyService, KeyService>();
            services.AddTransient<ISecretService, SecretService>();

            services.AddTransient<ICertificateBackingService, CertificateBackingService>();
            services.AddTransient<ICertificateService, CertificateService>();

            services.AddTransient<IEncryptionService, EncryptionService>();
            services.AddTransient<ITokenService, TokenService>();

            return services;
        }
    }
}
