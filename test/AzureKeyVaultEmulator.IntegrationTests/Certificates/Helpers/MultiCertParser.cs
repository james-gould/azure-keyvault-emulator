using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AzureKeyVaultEmulator.IntegrationTests.Certificates.Helpers;

internal static class MultiCertParser
{
    internal static string BuildPemBundleFromPfx(string pfx)
    {
        var pfxBytes = Convert.FromBase64String(pfx);
        var collection = X509CertificateLoader.LoadPkcs12Collection(pfxBytes, "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);

        var leaf = collection.FirstOrDefault(c => c.HasPrivateKey)
                   ?? throw new InvalidOperationException("No certificate with a private key found.");

        var privateKeyPem = ExportPrivateKeyPem(leaf);

        var pemBundleBuilder = new StringBuilder();
        foreach (var cert in BuildOrderedChain(collection, leaf))
        {
            pemBundleBuilder.AppendLine(cert.ExportCertificatePem().TrimEnd());
        }

        pemBundleBuilder.AppendLine(privateKeyPem.TrimEnd());

        // Clean up (collection contains cert handles)
        foreach (var c in collection) c.Dispose();

        return pemBundleBuilder.ToString();
    }

    static string ExportPrivateKeyPem(X509Certificate2 cert)
    {
        using RSA? rsa = cert.GetRSAPrivateKey();

        if (rsa is not null)
            return rsa.ExportPkcs8PrivateKeyPem();

        throw new NotSupportedException("Unsupported key type, expected RSA.");
    }

    static IEnumerable<X509Certificate2> BuildOrderedChain(
        X509Certificate2Collection store,
        X509Certificate2 leaf)
    {
        var ordered = new List<X509Certificate2> { leaf };
        var current = leaf;

        while (true)
        {
            var issuer = store.FirstOrDefault(c =>
                !ReferenceEquals(c, current) && current.IssuerName.RawData.SequenceEqual(c.SubjectName.RawData));

            if (issuer is null)
                break;

            ordered.Add(issuer);
            current = issuer;

            // Stop at root
            if (issuer.SubjectName.RawData.SequenceEqual(issuer.IssuerName.RawData))
                break;
        }

        return ordered;
    }
}
