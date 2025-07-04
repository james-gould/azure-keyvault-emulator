using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Emulator.Controllers
{
    [Route("")]
    public class EmulatorController(ITokenService token) : Controller
    {
        [HttpGet("token")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        public IActionResult GenerateStubToken()
        {
            var jwt = token.CreateBearerToken();

            return Ok(jwt);
        }

        [HttpGet("")]
        public IActionResult Root()
        {
            return Ok();
        }
    }
}
