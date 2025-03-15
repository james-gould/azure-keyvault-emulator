using System.ComponentModel.DataAnnotations;
using AzureKeyVaultEmulator.Keys.Services;
using AzureKeyVaultEmulator.Shared.Models.Keys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Keys.Controllers
{
    [ApiController]
    [Route("keys/{name}")]
    [Authorize]
    public class KeysController : ControllerBase
    {
        private readonly IKeyVaultKeyService _keyVaultKeyService;

        public KeysController(IKeyVaultKeyService keyVaultKeyService)
        {
            _keyVaultKeyService = keyVaultKeyService;
        }

        [HttpPost("create")]
        [Produces("application/json")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status200OK)]
        public IActionResult CreateKey(
            [RegularExpression("[a-zA-Z0-9-]+")][FromRoute] string name,
            [FromQuery(Name = "api-version")] string apiVersion,
            [FromBody] CreateKeyModel requestBody)
        {
            var createdKey = _keyVaultKeyService.CreateKey(name, requestBody);

            return Ok(createdKey);
        }

        [HttpGet("{version}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status200OK)]
        public IActionResult GetKey(
            [FromRoute] string name,
            [FromRoute] string version,
            [FromQuery(Name = "api-version")] string apiVersion)
        {
            var keyResult = _keyVaultKeyService.Get(name, version);

            if (keyResult == null) return NotFound();

            return Ok(keyResult);
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status200OK)]
        public IActionResult GetKey(
            [FromRoute] string name,
            [FromQuery(Name = "api-version")] string apiVersion)
        {
            var keyResult = _keyVaultKeyService.Get(name);

            if (keyResult == null) return NotFound();

            return Ok(keyResult);
        }

        [HttpPost("{version}/encrypt")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public IActionResult Encrypt(
            [FromRoute] string name,
            [FromRoute] string version,
            [FromQuery(Name = "api-version")] string apiVersion,
            [FromBody] KeyOperationParameters keyOperationParameters)
        {
            var result = _keyVaultKeyService.Encrypt(name, version, keyOperationParameters);

            return Ok(result);
        }

        [HttpPost("{version}/decrypt")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public IActionResult Decrypt(
            [FromRoute] string name,
            [FromRoute] string version,
            [FromQuery(Name = "api-version")] string apiVersion,
            [FromBody] KeyOperationParameters keyOperationParameters)
        {
            var result = _keyVaultKeyService.Decrypt(name, version, keyOperationParameters);

            return Ok(result);
        }
    }
}
