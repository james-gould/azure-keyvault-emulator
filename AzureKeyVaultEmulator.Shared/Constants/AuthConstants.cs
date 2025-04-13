using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace AzureKeyVaultEmulator.Shared.Constants
{
    public static class AuthConstants
    {
        private const string _issuerSigningKey = "VZboShdn5FpO2b2iHA7pzhJpmc24e8u9";

        public static Regex JwtRegex = new Regex("(^[\\w-]*\\.[\\w-]*\\.[\\w-]*$)", RegexOptions.Compiled);

        public const string EmulatorUri = "https://azure-keyvault-emulator.vault.azure.net";

        public const string EmulatorIss = "localazurekeyvault.localhost.com";

        public static readonly SymmetricSecurityKey SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_issuerSigningKey));
    }
}
