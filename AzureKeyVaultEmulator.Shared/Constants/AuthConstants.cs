using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Shared.Constants
{
    public static class AuthConstants
    {
        public const string IssuerSigningKey = "VZboShdn5FpO2b2iHA7pzhJpmc24e8u9";

        public static Regex JwtRegex = new Regex("(^[\\w-]*\\.[\\w-]*\\.[\\w-]*$)", RegexOptions.Compiled);
    }
}
