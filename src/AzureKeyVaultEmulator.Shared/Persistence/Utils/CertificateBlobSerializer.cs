using System.Security.Cryptography.X509Certificates;

public static class CertificateBlobSerializer
{
    public static byte[] Serialize(X509Certificate2 cert, string? password = null)
    {
        return cert.Export(
            X509ContentType.Pkcs12, // preserves private key for PFX
            password ?? string.Empty
        );
    }

    public static X509Certificate2 Deserialize(byte[] blob, string? password = null)
    {
        return X509CertificateLoader.LoadPkcs12(
            blob,
            password,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
    }
}
