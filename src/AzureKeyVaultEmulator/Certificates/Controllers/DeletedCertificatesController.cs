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
    public async Task<IActionResult> GetDeletedCertificates(
        [ApiVersion] string apiVersion,
        [FromQuery] bool includePending = true,
        [FromQuery] int maxResults = 25,
        [SkipToken] string skipToken = "")
    {
        var skipCount = 0;

        if (!string.IsNullOrEmpty(skipToken))
            skipCount = tokenService.DecodeSkipToken(skipToken);

        var result = await certService.GetDeletedCertificatesAsync(maxResults, skipCount);

        return Ok(result);
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> GetDeletedCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = await certService.GetDeletedCertificateAsync(name);

        return Accepted(result);
    }

    [HttpGet("{name}/pending")]
    public async Task<IActionResult> GetPendingDeletedCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = await certService.GetPendingDeletedCertificateAsync(name);

        return Ok(result);
    }

    [HttpPost("{name}/recover")]
    public async Task<IActionResult> StartRecoveringCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = await certService.RecoverCerticateAsync(name);

        return Ok(result);
    }

    [HttpPost("{name}/recover/pending")]
    public async Task<IActionResult> GetPendingRecoveringCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = await certService.GetPendingDeletedCertificateAsync(name);

        return Ok(result);
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> PurgeCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        await certService.PurgeDeletedCertificateAsync(name);

        return NoContent();
    }
}
