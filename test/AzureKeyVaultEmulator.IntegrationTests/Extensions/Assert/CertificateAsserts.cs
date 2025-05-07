using Azure.Security.KeyVault.Certificates;

namespace Xunit;

public partial class Assert
{
    /// <summary>
    /// Validates the two certificates are identical.
    /// </summary>
    /// <param name="sourceOfTruth">Source of truth.</param>
    /// <param name="comparativeCertificate">Comparitive</param>
    /// <param name="fromGet">Flag to toggle Id checks, to be used when <paramref name="comparativeCertificate"/></param>
    /// <remarks>
    /// <para>The parameter <paramref name="fromGet"/> helps to keep the test asserts consistent but not all certificates are made equal (get it?).</para>
    /// <para>In many scenarios a cert is created, exported and then reintroduced to test the introduction behaviour. When this happens the CertId, encryption key, URIs etc will change.</para>
    /// <para>In those scenarios we only want to validate that the non-unique metadata is equal, along with the cert contents.</para>
    /// </remarks>
    public static void CertificatesAreEqual(
        KeyVaultCertificateWithPolicy sourceOfTruth,
        KeyVaultCertificateWithPolicy comparativeCertificate,
        bool fromGet = true)
    {
        NotNull(sourceOfTruth);
        NotNull(comparativeCertificate);

        Equal(sourceOfTruth.Name, comparativeCertificate.Name);
        Equal(sourceOfTruth.Cer, comparativeCertificate.Cer);

        if (fromGet)
        {
            Equal(sourceOfTruth.Id, comparativeCertificate.Id);

            if (!string.IsNullOrEmpty(sourceOfTruth.SecretId.ToString()) || !string.IsNullOrEmpty(comparativeCertificate.SecretId.ToString()))
                Equal(sourceOfTruth.SecretId, comparativeCertificate.SecretId);

            if (!string.IsNullOrEmpty(sourceOfTruth.KeyId.ToString()) || !string.IsNullOrEmpty(comparativeCertificate.KeyId.ToString()))
                Equal(sourceOfTruth.KeyId, comparativeCertificate.KeyId);
        }
    }

    /// <summary>
    /// Validates the two certificates are identical.
    /// </summary>
    /// <param name="sourceOfTruth">Source of truth.</param>
    /// <param name="comparativeCertificate">Comparitive</param>
    /// <param name="fromGet">Flag to toggle Id checks, to be used when <paramref name="comparativeCertificate"/></param>
    /// <remarks>
    /// <para>The parameter <paramref name="fromGet"/> helps to keep the test asserts consistent but not all certificates are made equal (get it?).</para>
    /// <para>In many scenarios a cert is created, exported and then reintroduced to test the introduction behaviour. When this happens the CertId, encryption key, URIs etc will change.</para>
    /// <para>In those scenarios we only want to validate that the non-unique metadata is equal, along with the cert contents.</para>
    /// </remarks>
    public static void CertificatesAreEqual(
        KeyVaultCertificate sourceOfTruth,
        KeyVaultCertificateWithPolicy comparativeCertificate,
        bool fromGet = true)
    {
        NotNull(sourceOfTruth);
        NotNull(comparativeCertificate);

        Equal(sourceOfTruth.Name, comparativeCertificate.Name);
        Equal(sourceOfTruth.Cer, comparativeCertificate.Cer);

        if (fromGet)
        {
            Equal(sourceOfTruth.Id, comparativeCertificate.Id);

            if (!string.IsNullOrEmpty(sourceOfTruth.SecretId.ToString()) || !string.IsNullOrEmpty(comparativeCertificate.SecretId.ToString()))
                Equal(sourceOfTruth.SecretId, comparativeCertificate.SecretId);

            if (!string.IsNullOrEmpty(sourceOfTruth.KeyId.ToString()) || !string.IsNullOrEmpty(comparativeCertificate.KeyId.ToString()))
                Equal(sourceOfTruth.KeyId, comparativeCertificate.KeyId);
        }
    }

    public static void IssuersAreEqual(CertificateIssuer first, CertificateIssuer second, bool firstFromConfig = true)
    {
        Equivalent(first.AdministratorContacts, second.AdministratorContacts);

        Equal(first.Name, second.Name);
        Equal(first.AccountId, second.AccountId);
        Equal(first.Password, second.Password);
        Equal(first.Enabled, second.Enabled);
        Equal(first.Provider, second.Provider);

        if(firstFromConfig)
            NotEqual(first.Id, second.Id);
    }
}
