using AzureKeyVaultEmulator.Certificates.Services;
using AzureKeyVaultEmulator.Shared.Models.Certificates;
using AzureKeyVaultEmulator.Shared.Models.Certificates.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Certificates.Controllers;

[ApiController]
[Route("certificates")]
[Authorize]
// https://learn.microsoft.com/en-us/rest/api/keyvault/certificates/operation-groups
public class CertificatesController(
    ICertificateService certService,
    ICertificateBackingService backingService,
    ITokenService tokenService) : Controller
{
    [HttpPost("{name}/create")]
    public async Task<IActionResult> CreateCertificate(
        [FromRoute] string name,
        [FromBody] CreateCertificateRequest request,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = await certService.CreateCertificateAsync(name, request.Attributes, request.CertificatePolicy, request.Tags);

        return Accepted(result);
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> GetCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = await certService.GetCertificateAsync(name);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetCertificates(
        [ApiVersion] string apiVersion,
        [FromQuery] int maxResults = 25,
        [SkipToken] string skipToken = "")
    {
        int skipCount = 0;

        if (!string.IsNullOrEmpty(skipToken))
            skipCount = tokenService.DecodeSkipToken(skipToken);

        var result = await certService.GetCertificatesAsync(maxResults, skipCount);

        return Ok(result);
    }

    [HttpGet("{name}/pending")]
    public async Task<IActionResult> GetPendingCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = await certService.GetPendingCertificateAsync(name);

        return Ok(result);
    }

    /// <summary>
    /// Might not be needed, can't remember the exact flow. 
    /// </summary>
    [HttpGet("{name}/completed")]
    public async Task<IActionResult> GetCompletedCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = await certService.GetPendingCertificateAsync(name);

        return Ok(result);
    }

    [HttpPost("{name}/pending/merge")]
    public async Task<IActionResult> MergeCertificates(
        [FromRoute] string name,
        [FromBody] MergeCertificatesRequest request,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = await certService.MergeCertificatesAsync(name, request);

        return Accepted(result);
    }

    [HttpGet("{name}/policy")]
    public async Task<IActionResult> GetCertificatePolicy(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = await certService.GetCertificatePolicyAsync(name);

        return Ok(result);
    }

    [HttpPatch("{name}")]
    public async Task<IActionResult> UpdateCertificate(
        [FromRoute] string name,
        [FromBody] UpdateCertificateRequest request,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(request);

        var result = await certService.UpdateCertificateAsync(name, request);

        return Ok(result);
    }

    [HttpPatch("{name}/policy")]
    public async Task<IActionResult> UpdateCertificatePolicy(
        [FromRoute] string name,
        [FromBody] CertificatePolicy policy,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(policy);

        var result = await certService.UpdateCertificatePolicyAsync(name, policy);

        return Ok(result);
    }

    [HttpPost("{name}/backup")]
    public async Task<IActionResult> BackupCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        var result = await certService.BackupCertificateAsync(name);

        return Ok(result);
    }

    [HttpPost("restore")]
    public async Task<IActionResult> RestoreCertificate(
        [ApiVersion] string apiVersion,
        [FromBody] ValueModel<string> certBackup)
    {
        ArgumentNullException.ThrowIfNull(certBackup);

        var result = await certService.RestoreCertificateAsync(certBackup);

        return Ok(result);
    }

    [HttpGet("{name}/versions")]
    public async Task<IActionResult> GetCertificateVersions(
        [FromRoute] string name,
        [ApiVersion] string apiVersion,
        [FromQuery] int maxResults = 25,
        [SkipToken] string skipToken = "")
    {
        int skipCount = 0;

        if(!string.IsNullOrEmpty(skipToken))
            skipCount = tokenService.DecodeSkipToken(skipToken);

        var result = await certService.GetCertificateVersionsAsync(name, maxResults, skipCount);

        return Ok(result);
    }

    [HttpPost("{name}/import")]
    public async Task<IActionResult> ImportCertificate(
        [FromRoute] string name,
        [FromBody] ImportCertificateRequest? request,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(request);

        var result = await certService.ImportCertificateAsync(name, request);

        return Ok(result);
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = await certService.DeleteCertificateAsync(name);

        return Ok(result);
    }

    /// <summary>
    /// Regions below need to be refactored in separate controllers that respect the order of the endpoints.
    /// The long comment at the end of the controller explains why.
    /// </summary>

    #region issuers

    [HttpGet("issuers/{name}")]
    public async Task<IActionResult> GetCertificateIssuer(
    [FromRoute] string name,
    [ApiVersion] string apiVersion)
    {
        var result = await certService.GetCertificateIssuerAsync(name);

        return Ok(result);
    }

    [HttpPut("issuers/{name}")]
    public async Task<IActionResult> CreateCertificateIssuer(
        [FromRoute] string name,
        [FromBody] IssuerBundle bundle,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(bundle);

        var result = await backingService.CreateIssuerAsync(name, bundle);

        return Ok(result);
    }

    [HttpDelete("issuers/{name}")]
    public async Task<IActionResult> DeleteIssuers(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = await backingService.DeleteIssuerAsync(name);

        return Ok(result);
    }

    [HttpPatch("issuers/{name}")]
    public async Task<IActionResult> UpdateCertificateIssuer(
        [FromRoute] string name,
        [FromBody] IssuerBundle bundle,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(bundle);

        var result = await backingService.UpdateCertificateIssuerAsync(name, bundle);

        return Ok(result);
    }

    #endregion

    #region contacts

    [HttpPut("contacts")]
    public async Task<IActionResult> SetContacts(
        [FromBody] SetContactsRequest request,
        [ApiVersion] string apiVersion)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await backingService.SetContactInformationAsync(request);

        return Ok(result);
    }

    [HttpDelete("contacts")]
    public async Task<IActionResult> DeleteContacts(
        [ApiVersion] string apiVersion)
    {
        var result = await backingService.DeleteCertificateContactsAsync();

        return Ok(result);
    }

    [HttpGet("contacts")]
    public async Task<IActionResult> GetCertificateContacts(
        [ApiVersion] string apiVersion)
    {
        var result = await backingService.GetCertificateContactsAsync();

        return Ok(result);
    }

    #endregion

    // Due to {name}/{version} everywhere, and how ASP.NET Core handles routing
    // any HttpGet("{name}/someValue} will end up here.
    // {version} becomes the route param when passing a name, so {name}/policy hits here
    // unless we have a regex negating it below the actual {name}/policy action
    // Put this at the bottom of the controller so it stops picking up other requests.
    [HttpGet("{name}/{version}")]
    public async Task<IActionResult> GetCertificateByVersion(
        [FromRoute] string name,
        [FromRoute] string version,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(version);

        var result = await certService.GetCertificateAsync(name, version);

        return Ok(result);
    }
}
