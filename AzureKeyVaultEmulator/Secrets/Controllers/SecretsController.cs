using System.ComponentModel.DataAnnotations;
using AzureKeyVaultEmulator.Secrets.Services;
using AzureKeyVaultEmulator.Shared.Exceptions;
using AzureKeyVaultEmulator.Shared.Models;
using AzureKeyVaultEmulator.Shared.Models.Secrets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Secrets.Controllers
{
    [ApiController]
    [Route("secrets/{name}")]
    [Authorize]
    public class SecretsController : ControllerBase
    {
        private readonly IKeyVaultSecretService _keyVaultSecretService;

        public SecretsController(IKeyVaultSecretService keyVaultSecretService)
        {
            _keyVaultSecretService = keyVaultSecretService;
        }

        [HttpPut]
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

        [HttpGet("{version}")]
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

        [HttpGet]
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

        [HttpDelete]
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

        [HttpDelete("backup")]
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
    }
}
