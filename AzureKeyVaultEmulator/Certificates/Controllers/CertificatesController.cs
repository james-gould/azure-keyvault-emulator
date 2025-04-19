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

    [HttpGet("{name}")]
    public IActionResult GetCertificate(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var result = certService.GetCertificate(name);

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
