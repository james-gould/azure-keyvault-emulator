﻿using System.Security.Cryptography.X509Certificates;
using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;
using AzureKeyVaultEmulator.Shared.Models.Secrets;

namespace AzureKeyVaultEmulator.Certificates.Services;

public interface ICertificateBackingService
{
    Task<(KeyBundle backingKey, SecretBundle backingSecret)> GetBackingComponentsAsync(string certName, X509Certificate2? cert, string? password = null, CertificatePolicy? policy = null, X509ContentType contentType = X509ContentType.Pfx);

    Task<IssuerBundle> GetIssuerAsync(string name);
    Task<IssuerBundle> CreateIssuerAsync(string name, IssuerBundle bundle);
    Task<IssuerBundle> AllocateIssuerToCertificateAsync(string certName, IssuerBundle bundle);

    Task<IssuerBundle> UpdateCertificateIssuerAsync(string issuerName, IssuerBundle bundle);
    Task<IssuerBundle> DeleteIssuerAsync(string issuerName);

    Task<CertificateContacts> SetContactInformationAsync(SetContactsRequest request);
    Task<CertificateContacts> DeleteCertificateContactsAsync();
    Task<CertificateContacts> GetCertificateContactsAsync();
}
