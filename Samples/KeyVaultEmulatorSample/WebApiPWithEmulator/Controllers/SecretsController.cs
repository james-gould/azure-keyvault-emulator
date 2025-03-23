using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc;

namespace WebApiWithEmulator.Controllers
{
    [Route("secrets")]
    public class SecretsController : ControllerBase
    {
        private SecretClient _secretClient;
        public SecretsController(SecretClient secretClient)
        {
            _secretClient = secretClient;
        }

        [HttpGet("create")]
        public async Task<IActionResult> CreateSecret(
            [FromQuery] string name = "",
            [FromQuery] string value = "")
        {
            var secret = await _secretClient.SetSecretAsync(name, value);

            return Ok(secret);
        }

        [HttpGet("get")]
        public async Task<IActionResult> GetSecret([FromQuery] string name)
        {
            var secretFromContainer = await _secretClient.GetSecretAsync(name);

            return secretFromContainer is null ? NotFound($"Could not find secret with name {name}") : Ok(secretFromContainer);
        }
    }
}
