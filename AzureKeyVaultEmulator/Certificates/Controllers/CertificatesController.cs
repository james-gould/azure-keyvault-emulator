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
    ICertificateBackingService backingService) : Controller
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

    [HttpGet("{name}/pending")]
    public IActionResult GetPendingCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.GetPendingCertificate(name);

        return Ok(result);
    }

    [HttpGet("{name}/completed")]
    public IActionResult GetCompletedCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.GetPendingCertificate(name);

        return Ok(result);
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
        [FromBody] ValueModel<string>? certBackup)
    {
        ArgumentNullException.ThrowIfNull(certBackup);

        var result = certService.RestoreCertificate(certBackup);

        return Ok(result);
    }

    // Doesn't this look fantastic?
    // Due to {name}/{version} everywhere, and how ASP.NET Core handles routing
    // {version} becomes the route param when passing a name, so {name}/policy hits here
    // unless we have a regex negating it below the actual {name}/policy action
    // Put this at the bottom of the controller so it stops picking up other requests.
    [HttpGet("{name:regex(^(?!issuers$).+)}/{version:regex(^(?!pending$|completed$|policy$).+)}")]
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
