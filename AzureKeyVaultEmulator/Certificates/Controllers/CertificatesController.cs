using AzureKeyVaultEmulator.Certificates.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Certificates.Controllers;

[ApiController]
[Route("certificates")]
[Authorize]
public class CertificatesController(ICertificateService certService) : Controller
{
    [HttpGet("{name}")]
    public IActionResult GetCertificate(
        [FromRoute] string name,
        [FromRoute] string version,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var result = certService.GetCertificate(name, version);

        return Ok(result);
    }
}
