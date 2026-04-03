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
        /// The environment variable name used to pass the Azure AD tenant ID to the emulator.
        /// </summary>
        public const string TenantIdEnvVar = "TENANT_ID";

        public static readonly SymmetricSecurityKey SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_issuerSigningKey));
    }
}
