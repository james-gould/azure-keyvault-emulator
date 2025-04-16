using AzureKeyVaultEmulator.Keys.Services;
using AzureKeyVaultEmulator.Shared.Models.Keys.RequestModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Keys.Controllers;

[ApiController]
[Route("")]
[Authorize]
public class RngController(IKeyService keyService) : Controller
{
    [HttpPost("rng")]
    public IActionResult GetRandomBytes(
        [FromBody] RandomBytesRequest request,
        [ApiVersion] string apiVersion
        )
    {
        var result = keyService.GetRandomBytes(request.Count);

        return Ok(result);
    }
}
