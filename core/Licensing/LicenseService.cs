using core.Licensing.Models;

namespace core.Licensing;

public class LicenseService : ILicenseService
{
    private readonly IDomainsStore _domainsStore;

    public LicenseService(IDomainsStore domainsStore)
    {
        _domainsStore = domainsStore;
    }

    public async Task<LicenseValidationResult> ValidateAsync(string domain, string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(licenseKey))
        {
            return new LicenseValidationResult { Valid = false };
        }

        var normalizedDomain = NormalizeDomain(domain);
        var stored = await _domainsStore.FindByDomainAsync(normalizedDomain);

        if (stored == null)
        {
            return new LicenseValidationResult { Valid = false };
        }

        if (!string.Equals(stored.LicenseKey, licenseKey, StringComparison.Ordinal))
        {
            return new LicenseValidationResult { Valid = false };
        }

        if (stored.Status != DomainStatus.Active)
        {
            return new LicenseValidationResult { Valid = false, ExpiresAt = stored.ExpiresAt };
        }

        if (stored.ExpiresAt.HasValue && stored.ExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            return new LicenseValidationResult { Valid = false, ExpiresAt = stored.ExpiresAt };
        }

        return new LicenseValidationResult { Valid = true, ExpiresAt = stored.ExpiresAt };
    }

    private static string NormalizeDomain(string domain)
    {
        var trimmed = domain.Trim().ToLowerInvariant();

        if (trimmed.StartsWith("http://", StringComparison.Ordinal))
            trimmed = trimmed[7..];
        if (trimmed.StartsWith("https://", StringComparison.Ordinal))
            trimmed = trimmed[8..];

        var slashIndex = trimmed.IndexOf('/');
        if (slashIndex >= 0)
        {
            trimmed = trimmed[..slashIndex];
        }

        return trimmed;
    }
}
