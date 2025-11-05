using System.Net;

namespace AzureKeyVaultEmulator.Middleware
{
    public class KeyVaultErrorMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception e)
            {
                var req = context.Request;

                var error = new KeyVaultError
                {
                    Code = "Failed to perform request into Azure Key Vault Emulator",
                    InnerError = e.InnerException == null ? null : new KeyVaultError
                    {
                        Message = e.InnerException.Message,
                    },
                    Message = e.Message
                };
                var status = HttpStatusCode.BadRequest;

                if (e is ConflictedItemException conflictedItemException)
                {
                    error = new KeyVaultError()
                    {
                        Code = "Conflict",
                        Message = $"{conflictedItemException.ItemType} {conflictedItemException.Name} is currently in a deleted but recoverable state, and its name cannot be reused; in this state, the key can only be recovered or purged.",
                        InnerError = new KeyVaultError
                        {
                            Code = "ObjectIsDeletedButRecoverable"
                        },
                    };
                    status = HttpStatusCode.Conflict;
                }
                else if (e is MissingItemException)
                {
                    status = HttpStatusCode.NotFound;
                }

                context.Response.StatusCode = (int)status;
                await context.Response.WriteAsJsonAsync(new
                {
                    Error = error
                });

                return;
            }
        }
    }
}
