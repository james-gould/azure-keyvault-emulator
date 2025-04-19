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
public class CertificatesController(ICertificateService certService) : Controller
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

    // pending/completed must be above {version:regex...} due to ASP.NET Core's ordering with endpoint registration
    // separating these into another controller caused the same bugs to occur due to the above registration behaviour.

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

    [HttpGet("{name}/{version:regex(^(?!pending$|completed$).+)}")]
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
}
