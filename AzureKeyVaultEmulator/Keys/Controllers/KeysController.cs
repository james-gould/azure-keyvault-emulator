using System.ComponentModel.DataAnnotations;
using AzureKeyVaultEmulator.Keys.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// https://learn.microsoft.com/en-us/rest/api/keyvault/keys/operation-groups
namespace AzureKeyVaultEmulator.Keys.Controllers
{
    [ApiController]
    [Route("keys")]
    [Authorize]
    public class KeysController : ControllerBase
    {
        private readonly IKeyService _keyVaultKeyService;

        public KeysController(IKeyService keyVaultKeyService)
        {
            _keyVaultKeyService = keyVaultKeyService;
        }

        [HttpPost("{name}/create")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status200OK)]
        public IActionResult CreateKey(
            [RegularExpression("[a-zA-Z0-9-]+")][FromRoute] string name,
            [ApiVersion] string apiVersion,
            [FromBody] CreateKeyModel requestBody)
        {
            var createdKey = _keyVaultKeyService.CreateKey(name, requestBody);

            return Ok(createdKey);
        }

        [HttpGet("{name}/{version}")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status200OK)]
        public IActionResult GetKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion)
        {
            var keyResult = _keyVaultKeyService.Get(name, version);

            if (keyResult == null)
                return NotFound();

            return Ok(keyResult);
        }

        [HttpGet("{name}")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status200OK)]
        public IActionResult GetKey(
            [FromRoute] string name,
            [ApiVersion] string apiVersion)
        {
            var keyResult = _keyVaultKeyService.Get(name);

            if (keyResult == null)
                return NotFound();

            return Ok(keyResult);
        }

        [HttpPost("{name}/{version}/encrypt")]
        public IActionResult Encrypt(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion,
            [FromBody] KeyOperationParameters keyOperationParameters)
        {
            var result = _keyVaultKeyService.Encrypt(name, version, keyOperationParameters);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/decrypt")]
        public IActionResult Decrypt(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion,
            [FromBody] KeyOperationParameters keyOperationParameters)
        {
            var result = _keyVaultKeyService.Decrypt(name, version, keyOperationParameters);

            return Ok(result);
        }

        [HttpPost("{name}/backup")]
        public IActionResult BackupKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion)
        {
            var result = _keyVaultKeyService.BackupKey(name);

            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("restore")]
        public IActionResult BackupKey(
            [FromBody] string value,
            [ApiVersion] string apiVersion)
        {
            var result = _keyVaultKeyService.BackupKey(value);

            return Ok(result);
        }

        [HttpPost("rng")]
        public IActionResult GetRandomBytes(
            [FromBody] int count,
            [ApiVersion] string apiVersion)
        {
            var result = _keyVaultKeyService.GetRandomBytes(count);

            return Ok(result);
        }
    }
}
