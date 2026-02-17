using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace extension_api.Core;

public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var requestId = context.HttpContext.Items["RequestId"] as string ?? "unknown";

        _logger.LogError(context.Exception, "Unhandled exception for request {RequestId}", requestId);

        var (statusCode, message) = context.Exception switch
        {
            InvalidOperationException ex => (StatusCodes.Status400BadRequest, ex.Message),
            ArgumentException ex => (StatusCodes.Status400BadRequest, ex.Message),
            KeyNotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        context.Result = new ObjectResult(new
        {
            error = message,
            requestId
        })
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }
}
