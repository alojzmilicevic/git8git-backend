using core.Licensing.Models;

namespace core.Licensing;

public interface IDomainsStore
{
    Task<Domain?> FindByDomainAsync(string domain);
    Task<Domain?> FindByLicenseKeyAsync(string licenseKey);
    Task SaveAsync(Domain domain);
    Task DeleteAsync(string domain);
}
