using AzureKeyVaultEmulator.Secrets.Services;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// https://learn.microsoft.com/en-us/rest/api/keyvault/secrets/operation-groups
namespace AzureKeyVaultEmulator.Secrets.Controllers
{
    [ApiController]
    [Route("secrets")]
    [Authorize]
    public class SecretsController(ISecretService secretService, ITokenService tokenService) : ControllerBase
    {
        [HttpPut("{name}")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult SetSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion,
            [FromBody] SetSecretModel requestBody)
        {
            var secret = secretService.SetSecret(name, requestBody);

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
            var secretResult = secretService.Get(name, version);

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
            var secretResult = secretService.Get(name);

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
            var deletedBundle = secretService.DeleteSecret(name);

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
            var backupResult = secretService.BackupSecret(name);

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

            if (!string.IsNullOrEmpty(skipToken))
                skipCount = tokenService.DecodeSkipToken(skipToken);

            var currentVersionSet = secretService.GetSecretVersions(name, maxResults, skipCount);

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
                skipCount = tokenService.DecodeSkipToken(skipToken);

            var currentVersionSet = secretService.GetSecrets(maxResults, skipCount);

            return Ok(currentVersionSet);
        }

        [HttpPost("restore")]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult RestoreSecret(
            [ApiVersion] string apiVersion,
            [FromBody] ValueResponse? backup)
        {
            ArgumentNullException.ThrowIfNull(backup);

            var secret = secretService.RestoreSecret(backup.Value);

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

            var updatedAttributes = secretService.UpdateSecret(name, version, attributes);

            return Ok(updatedAttributes);
        }
    }
}
