using System.Security.Claims;

namespace AzureKeyVaultEmulator.Emulator.Services
{
    public interface ITokenService
    {
        string CreateBearerToken(IEnumerable<Claim> claims);
        string CreateSkipToken(int skipCount);
        int DecodeSkipToken(string skipToken);
    }
}
