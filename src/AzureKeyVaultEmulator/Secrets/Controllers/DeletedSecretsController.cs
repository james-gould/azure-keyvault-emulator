using AzureKeyVaultEmulator.Secrets.Services;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Secrets.Controllers
{
    [ApiController]
    [Route("deletedsecrets")]
    [Authorize]
    public class DeletedSecretsController(ISecretService secretService, ITokenService tokenService) : Controller
    {
        [HttpGet("{name}")]
        [Produces("application/json")]
        [ProducesResponseType<SecretBundle>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDeletedSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var bundle = await secretService.GetDeletedSecretAsync(name);

            return Ok(bundle);
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType<ListResult<SecretBundle>>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult GetDeletedSecrets(
            [ApiVersion] string apiVersion,
            [FromQuery] int maxResults = 25,
            [SkipToken] string skipToken = "")
        {
            var skipCount = 0;

            if (!string.IsNullOrEmpty(skipToken))
                skipCount = tokenService.DecodeSkipToken(skipToken);

            var deletedSecrets = secretService.GetDeletedSecrets(maxResults, skipCount);

            return Ok(deletedSecrets);
        }

        [HttpDelete("{name}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PurgeDeletedSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            await secretService.PurgeDeletedSecretAsync(name);

            return NoContent();
        }

        [HttpPost("{name}/recover")]
        [Produces("application/json")]
        [ProducesResponseType<ListResult<SecretBundle>>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RecoverDeletedSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var secret = await secretService.RecoverDeletedSecretAsync(name);

            return Ok(secret);
        }
    }
}
