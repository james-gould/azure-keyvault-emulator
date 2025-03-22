using AzureKeyVaultEmulator.Emulator.Services;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
            var claims = new[]
{
                new Claim(JwtRegisteredClaimNames.Sub, "localuser"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var jwt = _token.CreateBearerToken(claims);

            return Ok(jwt);
        }
    }
}
