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

        var subjectName = policy.BuildSubjectAlternativeName();

        if (subjectName is not null)
            request.CertificateExtensions.Add(subjectName);

        return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(365));
    }

    private static X509Extension? BuildSubjectAlternativeName(this CertificatePolicy? policy)
    {
        if (policy is null || policy.CertificateProperties is null)
            return null;

        var builder = new SubjectAlternativeNameBuilder();

        foreach(var ns in policy.CertificateProperties.DnsNames)
            builder.AddDnsName(ns);

        foreach(var email in  policy.CertificateProperties.Emails)
            builder.AddEmailAddress(email);

        foreach(var principal in policy.CertificateProperties.Upns)
            builder.AddUserPrincipalName(principal);

        return builder.Build();
    }
}
