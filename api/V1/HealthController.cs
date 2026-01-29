using Microsoft.AspNetCore.Mvc;

namespace api.V1;

[Route("health")]
public class HealthController : ApiController
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "ok" });
    }
}
