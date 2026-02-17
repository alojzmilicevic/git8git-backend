using core.Licensing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace extension_api.V1;

[Route("api/licenses")]
public class LicensesController(ILicenseService licenseService) : ApiController
{
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate([FromBody] LicenseValidateRequest request)
    {
        var result = await licenseService.ValidateAsync(request.Domain, request.LicenseKey);
        return Ok(new { valid = result.Valid, expiresAt = result.ExpiresAt });
    }
}

public class LicenseValidateRequest
{
    public string Domain { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
}
