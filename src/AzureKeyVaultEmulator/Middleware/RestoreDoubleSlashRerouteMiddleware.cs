using System.Text.RegularExpressions;

namespace AzureKeyVaultEmulator.Middleware;

public static class BodgeExtensions
{
    /// <summary>
    /// <para>Bypasses the SDK bug causing API calls with an additional slash to fail.</para>
    /// <para>For example {vaultUri}/certificates/restore comes out of the SDK as {vaultUri}/certificates//restore, this middleware corrects that.</para>
    /// <para>PR raised here: https://github.com/Azure/azure-sdk-for-net/pull/49496</para>
    /// </summary>
    /// <param name="application"></param>
    public static WebApplication RegisterDoubleSlashBodge(this WebApplication application)
    {
        application.UseMiddleware<RestoreDoubleSlashRerouteMiddleware>();
        application.UseRouting();

        return application;
    }
}

public class RestoreDoubleSlashRerouteMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var originalPath = context.Request.Path.Value;

        if (originalPath is not null && originalPath.Contains("//"))
        {
            var rerouted = Regex.Replace(originalPath, "/{2,}", "/");

            context.Request.Path = new PathString(rerouted);
        }

        await next(context);
    }
}
