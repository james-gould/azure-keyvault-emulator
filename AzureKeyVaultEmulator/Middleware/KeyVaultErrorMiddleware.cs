using AzureKeyVaultEmulator.Shared.Models;
using System.Net;

namespace AzureKeyVaultEmulator.Middleware
{
    public class KeyVaultErrorMiddleware
    {
        private RequestDelegate _next;

        public KeyVaultErrorMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception e)
            {
                var errorResponse = new KeyVaultError
                {
                    Code = "Failed to perform request into Azure Key Vault Emulator",
                    InnerError = e,
                    Message = e.Message
                };

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsJsonAsync(errorResponse);

                return;
            }
        }
    }
}
