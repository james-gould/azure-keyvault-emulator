using Google.Protobuf;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace AzureKeyVaultEmulator.Shared.Utilities
{
    public static class EncodingUtils
    {
        public static string Base64UrlEncode(this byte[]? bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);

            return new StringBuilder(Convert.ToBase64String(bytes)).Replace('+', '-').Replace('/', '_').Replace("=", "").ToString();
        }

        public static string Base64UrlEncode(this string str)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(str);

            var bytes = Encoding.UTF8.GetBytes(str);

            return Base64UrlEncode(bytes);
        }

        public static byte[] Base64UrlDecode(this string encoded)
        {
            encoded = new StringBuilder(encoded).Replace('-', '+').Replace('_', '/').Append('=', (encoded.Length % 4 == 0) ? 0 : 4 - (encoded.Length % 4)).ToString();

            return Convert.FromBase64String(encoded);
        }
    }
}
