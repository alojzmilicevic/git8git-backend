using Microsoft.AspNetCore.Mvc;

namespace api;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected string? GetRequestId()
    {
        return HttpContext.Items["RequestId"] as string;
    }

    protected IActionResult NotFoundResponse(string message)
    {
        return NotFound(new { error = message, requestId = GetRequestId() });
    }

    protected IActionResult BadRequestResponse(string message)
    {
        return BadRequest(new { error = message, requestId = GetRequestId() });
    }
}
