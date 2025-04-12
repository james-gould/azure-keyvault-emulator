using AzureKeyVaultEmulator.Keys.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Keys.Controllers;

[ApiController]
[Authorize]
public class DeletedKeysController(IKeyService keyService, ITokenService tokenService) : Controller
{
    [HttpDelete("keys/{name}")]
    public IActionResult DeleteKey(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        var result = keyService.DeleteKey(name);

        return Ok(result);
    }

    [HttpGet("deletedkeys/{name}")]
    public IActionResult GetDeletedKey(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        var result = keyService.GetDeletedKey(name);

        return Ok(result);
    }

    [HttpGet("deletedkeys")]
    public IActionResult GetDeletedKeys(
        [ApiVersion] string apiVersion,
        [FromQuery] int maxResults = 25,
        [SkipToken] string skipToken = "")
    {
        int skipCount = 0;

        if(!string.IsNullOrEmpty(skipToken))
            skipCount = tokenService.DecodeSkipToken(skipToken);

        var result = keyService.GetDeletedKeys(maxResults, skipCount);

        return Ok(result);
    }

    [HttpDelete("deletedkeys/{name}")]
    public IActionResult PurgeDeletedKey(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        keyService.PurgeDeletedKey(name);

        return NoContent();
    }

    [HttpPost("deletedkeys/{name}/recover")]
    public IActionResult RecoverDeletedKey(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        var result = keyService.RecoverDeletedKey(name);

        return Ok(result);
    }
}
