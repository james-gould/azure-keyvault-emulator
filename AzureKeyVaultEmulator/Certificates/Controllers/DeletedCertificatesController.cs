using AzureKeyVaultEmulator.Certificates.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Certificates.Controllers;

[Route("deletedcertificates")]
[ApiController]
[Authorize]
public class DeletedCertificatesController(
    ICertificateService certService,
    ITokenService tokenService) : Controller
{
    [HttpGet]
    public IActionResult GetDeletedCertificates(
        [ApiVersion] string apiVersion,
        [FromQuery] bool includePending = true,
        [FromQuery] int maxResults = 25,
        [SkipToken] string skipToken = "")
    {
        var skipCount = 0;

        if (!string.IsNullOrEmpty(skipToken))
            skipCount = tokenService.DecodeSkipToken(skipToken);

        var result = certService.GetDeletedCertificates(maxResults, skipCount);

        return Ok(result);
    }

    [HttpGet("{name}")]
    public IActionResult GetDeletedCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.GetDeletedCertificate(name);

        return Accepted(result);
    }

    [HttpGet("{name}/pending")]
    public IActionResult GetPendingDeletedCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.GetPendingDeletedCertificate(name);

        return Ok(result);
    }

    [HttpPost("{name}/recover")]
    public IActionResult StartRecoveringCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.RecoverCerticate(name);

        return Ok(result);
    }

    [HttpPost("{name}/recover/pending")]
    public IActionResult GetPendingRecoveringCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.GetPendingDeletedCertificate(name);

        return Ok(result);
    }

    [HttpDelete("{name}")]
    public IActionResult PurgeCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        certService.PurgeDeletedCertificate(name);

        return NoContent();
    }
}
