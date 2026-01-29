using System.Collections.Concurrent;
using core.Licensing;
using core.Licensing.Models;

namespace tests.Fakes;

public class FakeDomainsStore : IDomainsStore
{
    private readonly ConcurrentDictionary<string, Domain> _domains = new();

    public Task<Domain?> FindByDomainAsync(string domain)
    {
        _domains.TryGetValue(domain, out var value);
        return Task.FromResult(value);
    }

    public Task<Domain?> FindByLicenseKeyAsync(string licenseKey)
    {
        var domain = _domains.Values.FirstOrDefault(d => d.LicenseKey == licenseKey);
        return Task.FromResult(domain);
    }

    public Task SaveAsync(Domain domain)
    {
        _domains[domain.DomainName] = domain;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string domain)
    {
        _domains.TryRemove(domain, out _);
        return Task.CompletedTask;
    }

    public void Clear() => _domains.Clear();
}
