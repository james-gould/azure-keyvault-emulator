using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace AzureKeyVaultEmulator.Shared.Constants
{
    public static class AuthConstants
    {
        private const string _issuerSigningKey = "VZboShdn5FpO2b2iHA7pzhJpmc24e8u9";

        public static Regex JwtRegex = new Regex("(^[\\w-]*\\.[\\w-]*\\.[\\w-]*$)", RegexOptions.Compiled);

        public const string EmulatorName = "Azure Key Vault Emulator";

        public const string EmulatorUri = "https://azure-keyvault-emulator.vault.azure.net";

        public const string EmulatorIss = "localazurekeyvault.localhost.com";

        /// <summary>
        /// Placeholder tenant id used by the emulator when acting as its own OAuth2 / OIDC authority,
        /// and advertised back to clients via the <c>WWW-Authenticate</c> challenge header. This is a
        /// fixed GUID so MSAL's "tenant id must be a GUID or well-known name" guards are satisfied.
        /// </summary>
        public const string EmulatorTenantId = "a0c2a3f5-e1b3-4d6a-9c41-2cdd1f2c7e0f";

        /// <summary>
        /// Environment variable read at startup that — when set — overrides <see cref="EmulatorTenantId"/>
        /// in the <c>WWW-Authenticate</c> challenge. Mirrors the well-known <c>AZURE_TENANT_ID</c> name
        /// used by the Azure SDK so the Aspire integration can simply propagate the host's value.
        /// </summary>
        public const string TenantIdEnvironmentVariable = "AZURE_TENANT_ID";

        public static readonly SymmetricSecurityKey SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_issuerSigningKey));
    }
}
