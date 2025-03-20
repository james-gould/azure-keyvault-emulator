using System.ComponentModel.DataAnnotations;
using AzureKeyVaultEmulator.Emulator.Services;
using AzureKeyVaultEmulator.Secrets.Services;
using AzureKeyVaultEmulator.Shared.Exceptions;
using AzureKeyVaultEmulator.Shared.Models;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Secrets.Controllers
{
    [ApiController]
    [Route("secrets")]
    [Authorize]
    public class SecretsController : ControllerBase
    {
        private readonly IKeyVaultSecretService _keyVaultSecretService;
        private readonly ITokenService _token;

        public SecretsController(IKeyVaultSecretService keyVaultSecretService, ITokenService token)
        {
            _keyVaultSecretService = keyVaultSecretService;
            _token = token;
        }

        [HttpPut("{name}")]
        [Produces("application/json")]
        [Consumes("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult SetSecret(
            [FromRoute] string name,
            [FromQuery(Name = "api-version")] string apiVersion,
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
            [FromQuery(Name = "api-version")] string apiVersion)
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
            [FromQuery(Name = "api-version")] string apiVersion)
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
            [FromQuery] string apiVersion)
        {
            var deletedBundle = _keyVaultSecretService.DeleteSecret(name);

            return Ok(deletedBundle);
        }

        [HttpDelete("{name}/backup")]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult BackupSecret(
            [FromRoute] string name,
            [FromRoute] string apiVersion)
        {
            var backupResult = _keyVaultSecretService.BackupSecret(name);

            return Ok(backupResult);
        }

        [HttpGet("{name}/versions")]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<KeyVaultError>(StatusCodes.Status400BadRequest)]
        public IActionResult GetVersions(
            [FromRoute] string name,
            [FromQuery] string apiVersion,
            [FromQuery] int maxResults = 25,
            [FromQuery] string skipToken = "")
        {
            int skipCount = 0;

            if(!string.IsNullOrEmpty(skipToken))
                skipCount = _token.DecodeSkipToken(skipToken);

            var currentVersionSet = _keyVaultSecretService.GetSecretVersions(name, maxResults, skipCount);

            return Ok(currentVersionSet);
        }
    }
}
