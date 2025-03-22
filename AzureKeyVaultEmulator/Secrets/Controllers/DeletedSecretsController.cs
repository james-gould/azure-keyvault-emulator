using AzureKeyVaultEmulator.Emulator.Services;
using AzureKeyVaultEmulator.Secrets.Services;
using AzureKeyVaultEmulator.Shared.Models;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Utilities.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Secrets.Controllers
{
    [ApiController]
    [Route("deletedsecrets")]
    [Authorize]
    public class DeletedSecretsController : Controller
    {
        private readonly IKeyVaultSecretService _keyVaultSecretService;
        private readonly ITokenService _token;

        public DeletedSecretsController(IKeyVaultSecretService keyVaultSecretService, ITokenService token)
        {
            _keyVaultSecretService = keyVaultSecretService;
            _token = token;
        }

        [HttpGet("{name}")]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult GetDeletedSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var bundle = _keyVaultSecretService.GetDeletedSecret(name);

            return Ok(bundle);
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType<ListResult<SecretResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult GetDeletedSecrets(
            [ApiVersion] string apiVersion,
            [FromQuery] int maxResults = 25,
            [SkipToken] string skipToken = "")
        {
            var skipCount = 0;

            if(!string.IsNullOrEmpty(skipToken))
                skipCount = _token.DecodeSkipToken(skipToken);

            var deletedSecrets = _keyVaultSecretService.GetDeletedSecrets(maxResults, skipCount);

            return Ok(deletedSecrets);
        }

        [HttpDelete("{name}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult PurgeDeletedSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            _keyVaultSecretService.PurgeDeletedSecret(name);

            return NoContent();
        }

        [HttpPost("{name}/recover")]
        [Produces("application/json")]
        [ProducesResponseType<ListResult<SecretResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult RecoverDeletedSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var secret = _keyVaultSecretService.RecoverDeletedSecret(name);

            return Ok(secret);
        }
    }
}
