using System.Net;

public class DenyCloudflareMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly List<string> _paths = new() { "/swagger", "/dvrmanagement" };

    public DenyCloudflareMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (
            context.Request.Headers.ContainsKey("CF-Connecting-IP") &&
            _paths.Any(x => context.Request.Path.StartsWithSegments(x, StringComparison.OrdinalIgnoreCase))
        )
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return;
        }

        await _next(context);
    }
}
