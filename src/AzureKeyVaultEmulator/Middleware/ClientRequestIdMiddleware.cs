namespace AzureKeyVaultEmulator.Middleware
{
    public class ClientRequestIdMiddleware
    {
        private readonly RequestDelegate _next;

        public ClientRequestIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientRequestId = context.Request.Headers["x-ms-client-request-id"].FirstOrDefault();
            var returnHeaderFlag = context.Request.Headers["x-ms-return-client-request-id"].FirstOrDefault();

            if (!string.IsNullOrEmpty(clientRequestId) &&
                string.Equals(returnHeaderFlag, "true", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["x-ms-client-request-id"] = clientRequestId;
                    return Task.CompletedTask;
                });
            }

            await _next(context);
        }
    }
}
