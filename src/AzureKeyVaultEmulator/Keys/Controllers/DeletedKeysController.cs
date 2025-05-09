using AzureKeyVaultEmulator.Keys.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Keys.Controllers;

[ApiController]
[Authorize]
public class DeletedKeysController(IKeyService keyService, ITokenService tokenService) : Controller
{
    [HttpGet("deletedkeys/{name}")]
    public async Task<IActionResult> GetDeletedKey(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        var result = await keyService.GetDeletedKeyAsync(name);

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
    public async Task<IActionResult> PurgeDeletedKey(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        await keyService.PurgeDeletedKey(name);

        return NoContent();
    }

    [HttpPost("deletedkeys/{name}/recover")]
    public async Task<IActionResult> RecoverDeletedKey(
        [FromRoute] string name,
        [ApiVersion] string apiVersion)
    {
        var result = await keyService.RecoverDeletedKeyAsync(name);

        return Ok(result);
    }
}
