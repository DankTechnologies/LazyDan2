namespace LazyDan2.Middleware;

public class ErrorLoggingMiddleware
{
    private readonly ILogger<ErrorLoggingMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ErrorLoggingMiddleware(RequestDelegate next, ILogger<ErrorLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}