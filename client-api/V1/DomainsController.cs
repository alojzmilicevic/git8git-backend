using System.Security.Cryptography;
using core.Licensing;
using core.Licensing.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace client_api.V1;

[Route("api/domains")]
[Authorize]
public class DomainsController(IDomainsStore domainsStore) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var userId = User.FindFirst("userId")?.Value;
        if (userId == null)
            return Unauthorized();

        var domains = await domainsStore.FindByUserIdAsync(userId);
        return Ok(domains.Select(d => new DomainResponse
        {
            DomainName = d.DomainName,
            LicenseKey = d.LicenseKey,
            Status = d.Status.ToString(),
            ExpiresAt = d.ExpiresAt
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDomainRequest request)
    {
        var userId = User.FindFirst("userId")?.Value;
        if (userId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Domain))
            return BadRequestResponse("Domain is required");

        var existing = await domainsStore.FindByDomainAsync(request.Domain);
        if (existing != null)
            return BadRequestResponse("This domain is already registered");

        var licenseKey = $"LIC-{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}";

        var domain = new Domain
        {
            DomainName = request.Domain,
            LicenseKey = licenseKey,
            Status = DomainStatus.Active,
            UserId = userId,
            ExpiresAt = request.ExpiresAt
        };

        await domainsStore.SaveAsync(domain);

        return Created($"/api/domains/{domain.DomainName}", new DomainResponse
        {
            DomainName = domain.DomainName,
            LicenseKey = licenseKey,
            Status = domain.Status.ToString(),
            ExpiresAt = domain.ExpiresAt
        });
    }

    [HttpDelete("{domainName}")]
    public async Task<IActionResult> Delete(string domainName)
    {
        var userId = User.FindFirst("userId")?.Value;
        if (userId == null)
            return Unauthorized();

        var domain = await domainsStore.FindByDomainAsync(domainName);
        if (domain == null)
            return NotFoundResponse("Domain not found");

        if (domain.UserId != userId)
            return Unauthorized(new { error = "You do not own this domain" });

        await domainsStore.DeleteAsync(domainName);
        return NoContent();
    }

}

public class CreateDomainRequest
{
    public string Domain { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class DomainResponse
{
    public string DomainName { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
}
