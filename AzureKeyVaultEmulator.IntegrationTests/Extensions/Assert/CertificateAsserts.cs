using Azure.Security.KeyVault.Certificates;

namespace Xunit;

public partial class Assert
{
    public static void CertificatesAreEqual(KeyVaultCertificateWithPolicy first, KeyVaultCertificateWithPolicy second)
    {
        NotNull(first);
        NotNull(second);

        Equal(first.Id, second.Id);
        Equal(first.Name, second.Name);
        Equal(first.Cer, second.Cer);

        if(!string.IsNullOrEmpty(first.SecretId.ToString()) || !string.IsNullOrEmpty(second.SecretId.ToString()))
            Equal(first.SecretId, second.SecretId);

        if (!string.IsNullOrEmpty(first.KeyId.ToString()) || !string.IsNullOrEmpty(second.KeyId.ToString()))
            Equal(first.KeyId, second.KeyId);
    }

    public static void CertificatesAreEqual(KeyVaultCertificate first, KeyVaultCertificateWithPolicy second)
    {
        NotNull(first);
        NotNull(second);

        Equal(first.Id, second.Id);
        Equal(first.Name, second.Name);
        Equal(first.Cer, second.Cer);

        if (!string.IsNullOrEmpty(first.SecretId.ToString()) || !string.IsNullOrEmpty(second.SecretId.ToString()))
            Equal(first.SecretId, second.SecretId);

        if (!string.IsNullOrEmpty(first.KeyId.ToString()) || !string.IsNullOrEmpty(second.KeyId.ToString()))
            Equal(first.KeyId, second.KeyId);
    }
}
