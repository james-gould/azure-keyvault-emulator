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

        public static byte[] Base64UrlDecode(this string encoded)
        {
            encoded = new StringBuilder(encoded).Replace('-', '+').Replace('_', '/').Append('=', (encoded.Length % 4 == 0) ? 0 : 4 - (encoded.Length % 4)).ToString();

            return Convert.FromBase64String(encoded);
        }
    }
}
