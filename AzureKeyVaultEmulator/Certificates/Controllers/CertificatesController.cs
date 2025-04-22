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
    public IActionResult CreateCertificate(
        [FromRoute] string name,
        [FromBody] CreateCertificateRequest request,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.CreateCertificate(name, request.Attributes, request.CertificatePolicy);

        return Accepted(result);
    }

    [HttpGet("{name}")]
    public IActionResult GetCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.GetCertificate(name);

        return Ok(result);
    }

    [HttpGet]
    public IActionResult GetCertificates(
        [ApiVersion] string apiVersion,
        [FromQuery] int maxResults = 25,
        [SkipToken] string skipToken = "")
    {
        int skipCount = 0;

        if (!string.IsNullOrEmpty(skipToken))
            skipCount = tokenService.DecodeSkipToken(skipToken);

        var result = certService.GetCertificates(maxResults, skipCount);

        return Ok(result);
    }

    [HttpGet("{name}/pending")]
    public IActionResult GetPendingCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.GetPendingCertificate(name);

        return Ok(result);
    }

    /// <summary>
    /// Might not be needed, can't remember the exact flow. 
    /// </summary>
    [HttpGet("{name}/completed")]
    public IActionResult GetCompletedCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.GetPendingCertificate(name);

        return Ok(result);
    }

    [HttpPost("{name}/pending/merge")]
    public IActionResult MergeCertificates(
        [FromRoute] string name,
        [FromBody] MergeCertificatesRequest request,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.MergeCertificates(name, request);

        return Accepted(result);
    }

    [HttpGet("{name}/policy")]
    public IActionResult GetCertificatePolicy(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.GetCertificatePolicy(name);

        return Ok(result);
    }

    [HttpPatch("{name}/policy")]
    public IActionResult UpdateCertificatePolicy(
        [FromRoute] string name,
        [FromBody] CertificatePolicy policy,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(policy);

        var result = certService.UpdateCertificatePolicy(name, policy);

        return Ok(result);
    }

    [HttpPost("{name}/backup")]
    public IActionResult BackupCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        var result = certService.BackupCertificate(name);

        return Ok(result);
    }

    [HttpPost("restore")]
    public IActionResult RestoreCertificate(
        [ApiVersion] string apiVersion,
        [FromBody] ValueModel<string> certBackup)
    {
        ArgumentNullException.ThrowIfNull(certBackup);

        var result = certService.RestoreCertificate(certBackup);

        return Ok(result);
    }

    [HttpGet("{name}/versions")]
    public IActionResult GetCertificateVersions(
        [FromRoute] string name,
        [ApiVersion] string apiVersion,
        [FromQuery] int maxResults = 25,
        [SkipToken] string skipToken = "")
    {
        int skipCount = 0;

        if(!string.IsNullOrEmpty(skipToken))
            skipCount = tokenService.DecodeSkipToken(skipToken);

        var result = certService.GetCertificateVersions(name, maxResults, skipCount);

        return Ok(result);
    }

    [HttpPost("{name}/import")]
    public IActionResult ImportCertificate(
        [FromRoute] string name,
        [FromBody] ImportCertificateRequest? request,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(request);

        var result = certService.ImportCertificate(name, request);

        return Ok(result);
    }

    [HttpDelete("{name}")]
    public IActionResult DeleteCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.DeleteCertificate(name);

        return Ok(result);
    }

    /// <summary>
    /// Regions below need to be refactored in separate controllers that respect the order of the endpoints.
    /// The long comment at the end of the controller explains why.
    /// </summary>

    #region issuers

    [HttpGet("issuers/{name}")]
    public IActionResult GetCertificateIssuer(
    [FromRoute] string name,
    [ApiVersion] string apiVersion)
    {
        var result = certService.GetCertificateIssuer(name);

        return Ok(result);
    }

    [HttpPut("issuers/{name}")]
    public IActionResult CreateCertificateIssuer(
        [FromRoute] string name,
        [FromBody] IssuerBundle bundle,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(bundle);

        var result = backingService.PersistIssuerConfig(name, bundle);

        return Ok(result);
    }

    [HttpDelete("issuers/{name}")]
    public IActionResult DeleteIssuers(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = backingService.DeleteIssuer(name);

        return Ok(result);
    }

    [HttpPatch("issuers/{name}")]
    public IActionResult UpdateCertificateIssuer(
        [FromRoute] string name,
        [FromBody] IssuerBundle bundle,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(bundle);

        var result = backingService.UpdateCertificateIssuer(name, bundle);

        return Ok(result);
    }

    #endregion

    #region contacts

    [HttpPut("contacts")]
    public IActionResult SetContacts(
        [FromBody] SetContactsRequest request,
        [ApiVersion] string apiVersion)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = backingService.SetContactInformation(request);

        return Ok(result);
    }

    [HttpDelete("contacts")]
    public IActionResult DeleteContacts(
        [ApiVersion] string apiVersion)
    {
        var result = backingService.DeleteCertificateContacts();

        return Ok(result);
    }

    [HttpGet("contacts")]
    public IActionResult GetCertificateContacts(
        [ApiVersion] string apiVersion)
    {
        var result = backingService.GetCertificateContacts();

        return Ok(result);
    }

    #endregion

    // Due to {name}/{version} everywhere, and how ASP.NET Core handles routing
    // any HttpGet("{name}/someValue} will end up here.
    // {version} becomes the route param when passing a name, so {name}/policy hits here
    // unless we have a regex negating it below the actual {name}/policy action
    // Put this at the bottom of the controller so it stops picking up other requests.
    [HttpGet("{name}/{version}")]
    public IActionResult GetCertificateByVersion(
        [FromRoute] string name,
        [FromRoute] string version,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(version);

        var result = certService.GetCertificate(name, version);

        return Ok(result);
    }
}
