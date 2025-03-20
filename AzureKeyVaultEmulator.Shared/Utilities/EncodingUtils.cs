using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Shared.Utilities
{
    public static class EncodingUtils
    {
        public static string Base64UrlEncode<T>(this T? item) where T : class
        {
            ArgumentNullException.ThrowIfNull(item);

            var asString = item.ToString();

            ArgumentException.ThrowIfNullOrWhiteSpace(asString);

            return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(asString));
        }

        public static string Base64UrlDecode(string encoded)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(encoded);

            var bytes = WebEncoders.Base64UrlDecode(encoded);

            return Encoding.UTF8.GetString(bytes);
        }
    }
}
