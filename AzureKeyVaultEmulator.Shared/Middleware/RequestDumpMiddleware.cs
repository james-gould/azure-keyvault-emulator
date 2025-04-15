using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace AzureKeyVaultEmulator.Shared.Middleware;

public sealed class RequestDumpMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        context.Request.Body.Seek(0, SeekOrigin.Begin);

        using var sr = new StreamReader(context.Request.Body);
        var body = await sr.ReadToEndAsync();

        context.Request.Body.Position = 0;

        RequestDebugModel dump = new()
        {
            Path = context.Request.Host + context.Request.GetEncodedPathAndQuery(),
            Headers = context.Request.Headers.Select(x => $"Key: {x.Key}, Value: {x.Value}"),
            Body = body
        };

        var json = JsonSerializer.Serialize(dump);

        Debug.WriteLine(json);

        await next(context);
    }
}

public sealed class RequestDebugModel
{
    public string Path { get; set; } = string.Empty;
    public IEnumerable<string> Headers { get; set; } = [];
    public string Body { get; set; } = string.Empty;
}
