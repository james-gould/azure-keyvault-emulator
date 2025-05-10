using AzureKeyVaultEmulator.Secrets.Services;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using AzureKeyVaultEmulator.Shared.Models.Secrets.Requests;
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
        [ProducesResponseType<SecretBundle>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion,
            [FromBody] SetSecretRequest requestBody)
        {
            var secret = await secretService.SetSecretAsync(name, requestBody);

            return Ok(secret);
        }

        [HttpGet("{name}/{version}")]
        [Produces("application/json")]
        [ProducesResponseType<SecretBundle>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSecret(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentException.ThrowIfNullOrEmpty(version);

            var secretResult = await secretService.GetSecretAsync(name, version);

            return Ok(secretResult);
        }

        [HttpGet("{name}")]
        [Produces("application/json")]
        [ProducesResponseType<SecretBundle>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            var secretResult = await secretService.GetSecretAsync(name);

            return Ok(secretResult);
        }

        [HttpPatch("{name}/{version}")]
        public async Task<IActionResult> UpdateSecret(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion,
            [FromBody] SecretAttributesModel attributes)
        {
            var updated = await secretService.UpdateSecretAsync(name, version, attributes);

            return Ok(updated);
        }

        [HttpDelete("{name}")]
        [Produces("application/json")]
        [ProducesResponseType<SecretBundle>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var deletedBundle = await secretService.DeleteSecretAsync(name);

            return Ok(deletedBundle);
        }

        [HttpPost("{name}/backup")]
        [Produces("application/json")]
        [ProducesResponseType<SecretBundle>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BackupSecret(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var backupResult = await secretService.BackupSecretAsync(name);

            return Ok(backupResult);
        }

        [HttpGet("{name}/versions")]
        [Produces("application/json")]
        [ProducesResponseType<SecretBundle>(StatusCodes.Status200OK)]
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
        [ProducesResponseType<SecretBundle>(StatusCodes.Status200OK)]
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
        [ProducesResponseType<SecretBundle>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RestoreSecret(
            [ApiVersion] string apiVersion,
            [FromBody] ValueModel<string>? backup)
        {
            ArgumentNullException.ThrowIfNull(backup);

            var secret = await secretService.RestoreSecretAsync(backup.Value);

            return Ok(secret);
        }
    }
}
