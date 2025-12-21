using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AzureKeyVaultEmulator.Shared.Models.Certificates;

namespace AzureKeyVaultEmulator.Certificates.Factories;

public static class X509CertificateFactory
{
    public static X509Certificate2 BuildX509Certificate(string name, CertificatePolicy? policy)
    {
        int keySize = 2048;

        // Needs to be constrained to acceptable numbers, just fixing a bug quickly.
        if (policy?.KeyProperties?.KeySize is not null && policy.KeyProperties.KeySize > 0)
            keySize = policy.KeyProperties.KeySize;

        using var rsa = RSA.Create(keySize);
        rsa.ImportFromPem(RsaPem.FullPem);

        var certName = new X500DistinguishedName($"CN={name}");

        var request = new CertificateRequest(certName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions
            .Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DataEncipherment |
                X509KeyUsageFlags.KeyEncipherment |
                X509KeyUsageFlags.DigitalSignature,
                false)
            );

        var sans = policy.BuildSubjectAlternativeName();

        if (sans is not null)
            request.CertificateExtensions.Add(sans);

        return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(365));
    }

    public static X509Certificate2 MergeCertificates(X509Certificate2 baseCert, IEnumerable<string> mergedAsBase64)
    {
        ArgumentNullException.ThrowIfNull(baseCert);
        ArgumentNullException.ThrowIfNull(mergedAsBase64);

        foreach (var cert64 in mergedAsBase64)
        {
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            var cert = new X509Certificate2(Convert.FromBase64String(cert64));
#pragma warning restore SYSLIB0057 // Type or member is obsolete
            //var cert = X509CertificateLoader.LoadCertificate(Encoding.Default.GetBytes(cert64));

            // Should be handling the variety of keys available, but for now just use RSA.
            // Low priority, GH issue #108
            var privateKey = cert.GetRSAPrivateKey();

            ArgumentNullException.ThrowIfNull(privateKey);

            baseCert.CopyWithPrivateKey(privateKey);
        }

        return baseCert;
    }

    public static string CombinePemBundle(X509Certificate2Collection collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        var sb = new StringBuilder();

        foreach (var cert in collection)
            sb.Append(cert.ExportCertificatePem());

        return sb.ToString();
    }

    /// <summary>
    /// Imports an X.509 certificate from a base64-encoded byte array, optionally using a password, and returns the
    /// primary certificate.
    /// </summary>
    /// <remarks>This method attempts to import the certificate as a PFX/PKCS#12 file. If the certificate is
    /// password-protected, the password parameter must be provided. The returned collection may contain additional
    /// certificates, such as intermediate or root certificates, if present in the input data.</remarks>
    /// <param name="rawCert">A byte array containing the base64-encoded certificate data to import. Cannot be null or empty.</param>
    /// <param name="password">An optional password used to decrypt the certificate if it is password-protected. If the certificate requires a
    /// password, this parameter must not be null.</param>
    /// <param name="collection">When this method returns, contains a collection of all certificates found in the input data, or null if no
    /// collection was created.</param>
    /// <returns>An X509Certificate2 object representing the primary certificate imported from the provided data.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the input byte array is empty or if the certificate cannot be imported due to an incompatible format.</exception>
    public static X509Certificate2 ImportFromBase64(byte[] rawCert, string? password, out X509Certificate2Collection? collection)
    {
        ArgumentNullException.ThrowIfNull(rawCert);
        collection = null;

        var importKeys = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.DefaultKeySet;

        if (rawCert.Length == 0)
            throw new InvalidOperationException($"Cannot import empty certificate bytes");

        try
        {
            // PFX
            return X509CertificateLoader.LoadCertificate(rawCert);
        }
        catch { }

        // Failure might happen due to password being required, now we should validate it's not null and use it.
        ArgumentNullException.ThrowIfNull(password);

        try
        {
            collection = X509CertificateLoader.LoadPkcs12Collection(rawCert, password, importKeys);

            return X509CertificateLoader.LoadPkcs12(rawCert, password, importKeys);
        }
        catch { }

        throw new InvalidOperationException($"Failed to import certificate due to incompatible type.");
    }

    public static string ParseContentType(this X509ContentType contentType) => contentType switch
    {
        X509ContentType.Pfx => "application/x-pkcs12",
        X509ContentType.Cert => "application/x-pem-file",

        _ => throw new InvalidOperationException($"Certificate content type {contentType} is not supported.")
    };

    private static X509Extension? BuildSubjectAlternativeName(this CertificatePolicy? policy)
    {
        if (policy is null || policy.CertificateProperties is null)
            return null;

        var sans = policy.CertificateProperties.SubjectAlternativeNames;

        var builder = new SubjectAlternativeNameBuilder();

        foreach(var ns in sans.DnsNames)
            builder.AddDnsName(ns);

        foreach(var email in  sans.Emails)
            builder.AddEmailAddress(email);

        foreach(var principal in sans.Upns)
            builder.AddUserPrincipalName(principal);

        return builder.Build();
    }
}
