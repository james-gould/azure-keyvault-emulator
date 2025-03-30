using AzureKeyVaultEmulator.Emulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Emulator.Controllers
{
    [Route("")]
    public class EmulatorController : Controller
    {
        private ITokenService _token;

        public EmulatorController(ITokenService token)
        {
            _token = token;
        }

        [HttpGet("token")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        public IActionResult GenerateStubToken()
        {
            var jwt = _token.CreateBearerToken();

            return Ok(jwt);
        }
    }
}
