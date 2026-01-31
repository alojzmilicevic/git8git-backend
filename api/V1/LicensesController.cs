using System.Security.Cryptography;
using core.Licensing;
using core.Licensing.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.V1;

[Route("api/licenses")]
public class LicensesController(ILicenseService licenseService, IDomainsStore domainsStore) : ApiController
{
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate([FromBody] LicenseValidateRequest request)
    {
        var result = await licenseService.ValidateAsync(request.Domain, request.LicenseKey);
        return Ok(new { valid = result.Valid, expiresAt = result.ExpiresAt });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateLicenseRequest request)
    {
        var licenseKey = GenerateLicenseKey();
        
        var domain = new Domain
        {
            DomainName = request.Domain,
            LicenseKey = licenseKey,
            Status = DomainStatus.Active,
            ExpiresAt = request.ExpiresAt
        };

        await domainsStore.SaveAsync(domain);

        return Created($"/api/licenses/{licenseKey}", new LicenseResponse
        {
            LicenseKey = licenseKey,
            Domain = domain.DomainName,
            Status = domain.Status.ToString(),
            ExpiresAt = domain.ExpiresAt
        });
    }

    [HttpGet("{licenseKey}")]
    [Authorize]
    public async Task<IActionResult> Get(string licenseKey)
    {
        var domain = await domainsStore.FindByLicenseKeyAsync(licenseKey);
        if (domain == null)
            return NotFound(new { error = "License not found" });

        return Ok(new LicenseResponse
        {
            LicenseKey = domain.LicenseKey,
            Domain = domain.DomainName,
            Status = domain.Status.ToString(),
            ExpiresAt = domain.ExpiresAt
        });
    }

    [HttpPut("{licenseKey}")]
    [Authorize]
    public async Task<IActionResult> Update(string licenseKey, [FromBody] UpdateLicenseRequest request)
    {
        var domain = await domainsStore.FindByLicenseKeyAsync(licenseKey);
        if (domain == null)
            return NotFound(new { error = "License not found" });

        var oldDomainName = domain.DomainName;
        var domainChanged = !string.IsNullOrEmpty(request.Domain) && request.Domain != oldDomainName;

        if (domainChanged)
        {
            await domainsStore.DeleteAsync(oldDomainName);
            domain.DomainName = request.Domain!;
        }

        if (request.Status.HasValue)
            domain.Status = request.Status.Value;

        if (request.ExpiresAt.HasValue)
            domain.ExpiresAt = request.ExpiresAt.Value;

        await domainsStore.SaveAsync(domain);

        return Ok(new LicenseResponse
        {
            LicenseKey = domain.LicenseKey,
            Domain = domain.DomainName,
            Status = domain.Status.ToString(),
            ExpiresAt = domain.ExpiresAt
        });
    }

    private static string GenerateLicenseKey()
    {
        return $"LIC-{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}";
    }
}

public class LicenseValidateRequest
{
    public string Domain { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
}

public class CreateLicenseRequest
{
    public string Domain { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class UpdateLicenseRequest
{
    public string? Domain { get; set; }
    public DomainStatus? Status { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class LicenseResponse
{
    public string LicenseKey { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
}
