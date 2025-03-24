using AzureKeyVaultEmulator.Emulator.Services;
using AzureKeyVaultEmulator.Shared.Constants;

namespace AzureKeyVaultEmulator.Middleware
{
    public class ForcedBearerTokenMiddleware(RequestDelegate requestDelegate)
    {
        private RequestDelegate _next = requestDelegate;

        public async Task InvokeAsync(HttpContext context)
        {
            var authHeaders = context.Request.Headers.Authorization.Where(x => !string.IsNullOrEmpty(x)).ToList();

            var token = authHeaders.Where(x => AuthConstants.JwtRegex.IsMatch(x!));

            if (!token.Any())
            {
                var service = context.RequestServices.GetService<ITokenService>();

                if(service is null)
                    throw new InvalidOperationException($"Failed to scaffold {nameof(ITokenService)}.");

                var bearer = service.CreateBearerToken();

                var success = context.Request.Headers.TryAdd("Authorization", $"Bearer {bearer}");

                if (!success)
                    throw new InvalidOperationException($"Failed to force insert Bearer token");
            }

            await _next(context);
        }
    }
}
