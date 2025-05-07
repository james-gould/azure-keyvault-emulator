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
            var cert = new X509Certificate2(Encoding.UTF8.GetBytes(cert64));
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

    public static X509Certificate2 ImportFromBase64(string certificateBase64)
    {
        ArgumentException.ThrowIfNullOrEmpty(certificateBase64);

        return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(certificateBase64));
    }

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
