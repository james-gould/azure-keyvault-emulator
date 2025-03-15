using System.ComponentModel.DataAnnotations;
using AzureKeyVaultEmulator.Secrets.Services;
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
        public IActionResult SetSecret(
            [RegularExpression("[a-zA-Z0-9-]+")][FromRoute] string name,
            [FromQuery(Name = "api-version")] string apiVersion,
            [FromBody] SetSecretModel requestBody)
        {
            var secret = _keyVaultSecretService.SetSecret(name, requestBody);

            return Ok(secret);
        }

        [HttpGet("{version}")]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        public IActionResult GetSecret(
            [FromRoute] string name,
            [FromRoute] string version,
            [FromQuery(Name = "api-version")] string apiVersion)
        {
            var secretResult = _keyVaultSecretService.Get(name, version);

            if (secretResult == null) return NotFound();

            return Ok(secretResult);
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType<SecretResponse>(StatusCodes.Status200OK)]
        public IActionResult GetSecret(
            [FromRoute] string name,
            [FromQuery(Name = "api-version")] string apiVersion)
        {
            var secretResult = _keyVaultSecretService.Get(name);

            if (secretResult == null) return NotFound();

            return Ok(secretResult);
        }
    }
}
