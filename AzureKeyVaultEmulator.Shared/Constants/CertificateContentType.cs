using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace AzureKeyVaultEmulator.Shared.Constants;

public static class CertificateContentType
{
    private static Regex _typeRegex = new Regex(@"^application\/([^\/\s]+)$", RegexOptions.Compiled);

    // https://pki-tutorial.readthedocs.io/en/latest/mime.html
    private static Dictionary<X509ContentType, string> _contentTypes = new()
    {
        { X509ContentType.Unknown, "unknown" },

        // not relevant, but should be handled
        { X509ContentType.SerializedCert, "unknown" },
        { X509ContentType.SerializedStore, "unknown" },

        // actually used content types from schema above
        { X509ContentType.Cert, "pkix-cert" },
        { X509ContentType.Pfx, "x-pkcs12" },
        { X509ContentType.Pkcs7, "x-pkcs7-crl" },
        { X509ContentType.Pkcs12, "x-pkcs12" },

        // no idea, need to generate an Authenticode cert and verify manually.
        // Used for code signing, possibly a tiny/obselete use-case for the emulator
        { X509ContentType.Authenticode, "x-authenticode" }
    };

    public static string ToApplicationContentType(this X509ContentType contentType)
    {
        return $"application/{_contentTypes[contentType]}";
    }

    public static X509ContentType FromApplicationContentType(this string contentType)
    {
        var matches = _typeRegex.Matches(contentType);

        if(matches.Count == 0)
            return X509ContentType.Unknown;

        return _contentTypes.Where(x => x.Value == matches[0].Value).FirstOrDefault().Key;
    }
}
