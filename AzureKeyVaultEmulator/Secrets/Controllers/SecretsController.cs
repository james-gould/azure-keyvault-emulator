using AzureKeyVaultEmulator.Secrets.Services;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Utilities.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Secrets.Controllers
{
    [ApiController]
    [Route("secrets")]
    [Authorize]
    public class SecretsController : ControllerBase
    {
        private readonly ISecretService _keyVaultSecretService;
        private readonly ITokenService _token;

        public SecretsController(ISecretService keyVaultSecretService, ITokenService token)
        {
            _keyVaultSecretService = keyVaultSecretService;
            _token = token;
        }

        [HttpPut("{name}")]
        //[Produces("application/json")]
        //[Consumes("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult SetSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion,
            [FromBody] SetSecretModel requestBody)
        {
            var secret = _keyVaultSecretService.SetSecret(name, requestBody);

            return Ok(secret);
        }

        [HttpGet("{name}/{version}")]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult GetSecret(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion)
        {
            var secretResult = _keyVaultSecretService.Get(name, version);

            return Ok(secretResult);
        }

        [HttpGet("{name}")]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult GetSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var secretResult = _keyVaultSecretService.Get(name);

            return Ok(secretResult);
        }

        [HttpDelete("{name}")]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult DeleteSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var deletedBundle = _keyVaultSecretService.DeleteSecret(name);

            return Ok(deletedBundle);
        }

        [HttpPost("{name}/backup")]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult BackupSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var backupResult = _keyVaultSecretService.BackupSecret(name);

            return Ok(backupResult);
        }

        [HttpGet("{name}/versions")]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult GetSecretVersions(
            [FromRoute] string name,
            [ApiVersion] string apiVersion,
            [FromQuery] int maxResults = 25,
            [SkipToken] string skipToken = "")
        {
            int skipCount = 0;

            if(!string.IsNullOrEmpty(skipToken))
                skipCount = _token.DecodeSkipToken(skipToken);

            var currentVersionSet = _keyVaultSecretService.GetSecretVersions(name, maxResults, skipCount);

            return Ok(currentVersionSet);
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult GetSecrets(
            [ApiVersion] string apiVersion,
            [FromQuery] int maxResults = 25,
            [SkipToken] string skipToken = "")
        {
            int skipCount = 0;

            if (!string.IsNullOrEmpty(skipToken))
                skipCount = _token.DecodeSkipToken(skipToken);

            var currentVersionSet = _keyVaultSecretService.GetSecrets(maxResults, skipCount);

            return Ok(currentVersionSet);
        }

        [HttpPost("restore")]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult RestoreSecret(
            [ApiVersion] string apiVersion,
            [FromBody] BackupSecretResult? backup)
        {
            ArgumentNullException.ThrowIfNull(backup);

            var secret = _keyVaultSecretService.RestoreSecret(backup.Value);

            return Ok(secret);
        }

        [HttpPatch("{name}/{version}")]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult UpdateSecret(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion,
            [FromBody] SecretAttributesModel attributes)
        {
            ArgumentNullException.ThrowIfNull(attributes);

            var updatedAttributes = _keyVaultSecretService.UpdateSecret(name, version, attributes);

            return Ok(updatedAttributes);
        }
    }
}
