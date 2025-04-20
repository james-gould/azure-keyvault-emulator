namespace AzureKeyVaultEmulator.Middleware;

/// <summary>
/// <para>Bypasses the SDK bug causing /restore API calls to fail.</para>
/// <para>PR raised here: https://github.com/Azure/azure-sdk-for-net/pull/49496</para>
/// </summary>
/// <param name="next"></param>
public class RestoreDoubleSlashRerouteMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.ToString();

        if (path.Contains("//"))
        {
            path = path.Replace("//", "/");

            context.Request.Path = path;
        }

        await next(context);
    }
}
