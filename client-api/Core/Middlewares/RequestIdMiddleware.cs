namespace client_api.Core.Middlewares;

public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestIdMiddleware> _logger;

    public RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString();

        context.Items["RequestId"] = requestId;
        context.Response.Headers["X-Request-Id"] = requestId;

        using (_logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId }))
        {
            _logger.LogInformation("Request started: {Method} {Path}", context.Request.Method, context.Request.Path);

            var startTime = DateTime.UtcNow;
            await _next(context);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Request completed: {StatusCode} in {Duration}ms",
                context.Response.StatusCode, duration.TotalMilliseconds);
        }
    }
}
