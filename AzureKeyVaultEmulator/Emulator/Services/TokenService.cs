using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AzureKeyVaultEmulator.Emulator.Services
{
    public interface ITokenService
    {
        string CreateBearerToken(IEnumerable<Claim>? inboundClaims = null);
        string CreateSkipToken(int skipCount);
        int DecodeSkipToken(string skipToken);
        string CreateTokenWithHeaderClaim(IEnumerable<Claim> payloadClaims, string headerClaimType, string headerClaimValue);
    }

    public class TokenService : ITokenService
    {
        private const string _skipClaim = "skipCount";

        public string CreateBearerToken(IEnumerable<Claim>? inboundClaims = null)
        {
            if (inboundClaims is null)
                inboundClaims = [];

            var claims = new[]
{
                new Claim(JwtRegisteredClaimNames.Sub, "localuser"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            return CreateToken([.. inboundClaims, .. claims]);
        }

        public string CreateSkipToken(int skipCount)
        {
            var claim = new Claim(_skipClaim, $"{skipCount}");

            return CreateToken([claim]);
        }

        public int DecodeSkipToken(string skipToken)
        {
            var token = new JwtSecurityToken(skipToken);

            var skipClaim = token.Claims.FirstOrDefault(x => x.Type.Equals(_skipClaim, StringComparison.OrdinalIgnoreCase));

            if (skipClaim is null)
                return default;

            var validSkipClaim = int.TryParse(skipClaim.Value, out int skipCount);

            return validSkipClaim ? skipCount : default;
        }

        public string CreateTokenWithHeaderClaim(
            IEnumerable<Claim> payloadClaims,
            string headerClaimType,
            string headerClaimValue)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthConstants.IssuerSigningKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "localazurekeyvault.localhost.com",
                audience: "localazurekeyvault.localhost.com",
                claims: [.. payloadClaims],
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            token.Header.Add(headerClaimType, headerClaimValue);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string CreateToken(IEnumerable<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthConstants.IssuerSigningKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "localazurekeyvault.localhost.com",
                audience: "localazurekeyvault.localhost.com",
                claims: [.. claims],
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
