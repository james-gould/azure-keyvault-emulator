using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AzureKeyVaultEmulator.Shared.Models.Certificates;

namespace AzureKeyVaultEmulator.Certificates.Factories;

public static class X509CertificateFactory
{
    public static X509Certificate2 BuildX509Certificate(string name, CertificatePolicy? policy)
    {
        using var rsa = RSA.Create(policy?.KeyProperties?.KeySize ?? 2048);

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

    private static X509Extension? BuildSubjectAlternativeName(this CertificatePolicy? policy)
    {
        if (policy is null || policy.CertificateProperties is null)
            return null;

        var builder = new SubjectAlternativeNameBuilder();

        var sans = policy.CertificateProperties.SubjectAlternativeNames;

        foreach(var ns in sans.DnsNames)
            builder.AddDnsName(ns);

        foreach(var email in  sans.Emails)
            builder.AddEmailAddress(email);

        foreach(var principal in sans.Upns)
            builder.AddUserPrincipalName(principal);

        return builder.Build();
    }
}
