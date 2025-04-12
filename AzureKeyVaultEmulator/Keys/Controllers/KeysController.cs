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
    public class KeysController(IKeyService keyService) : ControllerBase
    {
        [HttpPost("{name}/create")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status200OK)]
        public IActionResult CreateKey(
            [RegularExpression("[a-zA-Z0-9-]+")][FromRoute] string name,
            [ApiVersion] string apiVersion,
            [FromBody] CreateKeyModel requestBody)
        {
            var createdKey = keyService.CreateKey(name, requestBody);

            return Ok(createdKey);
        }

        [HttpGet("{name}/{version}")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status200OK)]
        public IActionResult GetKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion)
        {
            var keyResult = keyService.GetKey(name, version);

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
            var keyResult = keyService.GetKey(name);

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
            var result = keyService.Encrypt(name, version, keyOperationParameters);

            return Ok(result);
        }

        [HttpPost("{name}/{version}/decrypt")]
        public IActionResult Decrypt(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion,
            [FromBody] KeyOperationParameters keyOperationParameters)
        {
            var result = keyService.Decrypt(name, version, keyOperationParameters);

            return Ok(result);
        }

        [HttpPost("{name}/backup")]
        public IActionResult BackupKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [ApiVersion] string apiVersion)
        {
            var result = keyService.BackupKey(name);

            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("restore")]
        public IActionResult BackupKey(
            [FromBody] string value,
            [ApiVersion] string apiVersion)
        {
            var result = keyService.BackupKey(value);

            return Ok(result);
        }

        [HttpPost("rng")]
        public IActionResult GetRandomBytes(
            [FromBody] int count,
            [ApiVersion] string apiVersion)
        {
            var result = keyService.GetRandomBytes(count);

            return Ok(result);
        }

        [HttpGet("{name}/rotationpolicy")]
        public IActionResult GetKeyRotationPolicy(
            [FromRoute] string name)
        {
            var result = keyService.GetKeyRotationPolicy(name);

            return Ok(result);
        }

        [HttpPut("{name}/rotationpolicy")]
        public IActionResult UpdateKeyRotationPolicy(
            [FromRoute] string name,
            [FromBody] KeyRotationAttributes attributes,
            [FromBody] IEnumerable<LifetimeActions> lifetimeActions,
            [ApiVersion] string apiVersion)
        {
            var result = keyService.UpdateKeyRotationPolicy(name, attributes, lifetimeActions);

            return Ok(result);
        }
    }
}
