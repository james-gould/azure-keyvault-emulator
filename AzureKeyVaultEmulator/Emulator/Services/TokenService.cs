using AzureKeyVaultEmulator.Shared.Constants;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AzureKeyVaultEmulator.Emulator.Services
{
    public interface ITokenService
    {
        string CreateBearerToken(IEnumerable<Claim> claims);
        string CreateSkipToken(int skipCount);
        int DecodeSkipToken(string skipToken);
    }

    public class TokenService : ITokenService
    {
        private const string _skipClaim = "skipCount";

        public string CreateBearerToken(IEnumerable<Claim> claims)
        {
            return CreateToken(claims);
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
