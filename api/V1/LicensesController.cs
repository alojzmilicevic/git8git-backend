using core.Licensing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.V1;

[Route("api/licenses")]
public class LicensesController : ApiController
{
    private readonly ILicenseService _licenseService;

    public LicensesController(ILicenseService licenseService)
    {
        _licenseService = licenseService;
    }

    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate([FromBody] LicenseValidateRequest request)
    {
        var result = await _licenseService.ValidateAsync(request.Domain, request.LicenseKey);
        return Ok(new { valid = result.Valid, expiresAt = result.ExpiresAt });
    }
}

public class LicenseValidateRequest
{
    public string Domain { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
}
