using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AzureKeyVaultEmulator.IntegrationTests.Certificates.Helpers;

internal static class MultiCertGenerator
{
    public sealed record Result(
        byte[] PfxBytes,
        string CombinedPem,
        X509Certificate2 RootCa,
        X509Certificate2 IntermediateCa,
        X509Certificate2 LeafWithPrivateKey);

    public static Result Generate(string cName)
    {
        // --- Keys ---
        using var rootKey = RSA.Create(4096);
        using var intermediateKey = RSA.Create(4096);
        using var leafKey = RSA.Create(2048);

        // --- Root CA (self-signed) ---
        var rootCert = CreateRootCa(rootKey, $"CN={cName} Root CA");

        // --- Intermediate CA (signed by RootKey) ---
        var intermediateCert = CreateIntermediateCa(
            intermediateKey,
            issuerName: rootCert.SubjectName,
            issuerKey: rootKey,
            subject: $"CN={cName} Intermediate CA");

        // --- Leaf cert (signed by IntermediateKey) ---
        var leafCertWithKey = CreateLeafCert(
            leafKey,
            issuerName: intermediateCert.SubjectName,
            issuerKey: intermediateKey,
            subject: $"CN={cName}");

        // --- Combined PEM: leaf private key + leaf + intermediate + root ---
        var combinedPem =
            leafKey.ExportPkcs8PrivateKeyPem() +
            leafCertWithKey.ExportCertificatePem() +
            intermediateCert.ExportCertificatePem() +
            rootCert.ExportCertificatePem();

        // --- PFX bytes with NO password ---
        // Put leaf(with private key) first, then add chain certs.
        var collection = new X509Certificate2Collection
        {
            leafCertWithKey,
            intermediateCert,
            rootCert
        };

        var pfxBytes = collection.Export(X509ContentType.Pfx); // NO PASSWORD

        return new Result(
            PfxBytes: pfxBytes ?? [],
            CombinedPem: combinedPem,
            RootCa: rootCert,
            IntermediateCa: intermediateCert,
            LeafWithPrivateKey: leafCertWithKey);
    }

    private static X509Certificate2 CreateRootCa(RSA rootKey, string subject)
    {
        var req = new CertificateRequest(
            new X500DistinguishedName(subject),
            rootKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        req.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: true,
                hasPathLengthConstraint: true,
                pathLengthConstraint: 1,
                critical: true));

        req.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                critical: true));

        req.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = notBefore.AddYears(15);

        // Self-signed root. This may or may not "carry" the private key association,
        // but we don't rely on it for signing anyway (we keep rootKey separately).
        using var root = req.CreateSelfSigned(notBefore, notAfter);

        // Return a stable cert (no private key needed on this object)
#pragma warning disable SYSLIB0057 // Type or member is obsolete
        return new X509Certificate2(
            root.Export(X509ContentType.Cert),
            (string?)null,
            X509KeyStorageFlags.EphemeralKeySet);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
    }

    private static X509Certificate2 CreateIntermediateCa(
        RSA intermediateKey,
        X500DistinguishedName issuerName,
        RSA issuerKey,
        string subject)
    {
        var req = new CertificateRequest(
            new X500DistinguishedName(subject),
            intermediateKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        req.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: true,
                hasPathLengthConstraint: true,
                pathLengthConstraint: 0,
                critical: true));

        req.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                critical: true));

        req.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = notBefore.AddYears(10);

        var serial = RandomSerialNumber();

        // Sign using issuerKey directly (issuer cert does NOT need a private key attached).
        var generator = X509SignatureGenerator.CreateForRSA(issuerKey, RSASignaturePadding.Pkcs1);

        using var issued = req.Create(
            issuerName: issuerName,
            generator: generator,
            notBefore: notBefore,
            notAfter: notAfter,
            serialNumber: serial);

        // We return the intermediate cert *without* private key attached (not needed for this POC),
        // because we keep intermediateKey for signing leaf. This also avoids tricky provider export issues.
#pragma warning disable SYSLIB0057 // Type or member is obsolete
        return new X509Certificate2(
            issued.Export(X509ContentType.Cert),
            (string?)null,
            X509KeyStorageFlags.EphemeralKeySet);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
    }

    private static X509Certificate2 CreateLeafCert(
        RSA leafKey,
        X500DistinguishedName issuerName,
        RSA issuerKey,
        string subject)
    {
        var req = new CertificateRequest(
            new X500DistinguishedName(subject),
            leafKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        req.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: false,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: true));

        req.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: true));

        // EKU: Server Authentication (optional)
        req.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") },
                critical: false));

        req.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

        // Optional SAN (handy for TLS testing)
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        req.CertificateExtensions.Add(san.Build());

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = notBefore.AddYears(2);

        var serial = RandomSerialNumber();

        var generator = X509SignatureGenerator.CreateForRSA(issuerKey, RSASignaturePadding.Pkcs1);

        using var issued = req.Create(
            issuerName: issuerName,
            generator: generator,
            notBefore: notBefore,
            notAfter: notAfter,
            serialNumber: serial);

        // Attach leaf private key (this one MUST be in the PFX for Key Vault import).
        using var withKey = issued.CopyWithPrivateKey(leafKey);

        // Return stable in-memory cert with private key (exportable for PFX export)
#pragma warning disable SYSLIB0057 // Type or member is obsolete
        return new X509Certificate2(
            withKey.Export(X509ContentType.Pfx),
            (string?)null,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
    }

    private static byte[] RandomSerialNumber()
    {
        var serial = new byte[16];
        RandomNumberGenerator.Fill(serial);
        serial[0] &= 0x7F; // positive
        return serial;
    }
}

