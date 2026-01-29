using core.Licensing.Models;
using core.Storage.Dynamo;

namespace core.Licensing;

public class DomainsStore : IDomainsStore
{
    private readonly IDynamoDb _dynamoDb;

    public DomainsStore(IDynamoDb dynamoDb)
    {
        _dynamoDb = dynamoDb;
    }

    public async Task<Domain?> FindByDomainAsync(string domain)
    {
        var normalized = NormalizeDomain(domain);
        return await _dynamoDb.Context.LoadAsync<Domain>(normalized);
    }

    public async Task<Domain?> FindByLicenseKeyAsync(string licenseKey)
    {
        var search = _dynamoDb.Context.ScanAsync<Domain>(
            new[] { new Amazon.DynamoDBv2.DataModel.ScanCondition("LicenseKey", Amazon.DynamoDBv2.DocumentModel.ScanOperator.Equal, licenseKey) });
        var results = await search.GetRemainingAsync();
        return results.FirstOrDefault();
    }

    public async Task SaveAsync(Domain domain)
    {
        domain.DomainName = NormalizeDomain(domain.DomainName);
        await _dynamoDb.Context.SaveAsync(domain);
    }

    public async Task DeleteAsync(string domain)
    {
        var normalized = NormalizeDomain(domain);
        await _dynamoDb.Context.DeleteAsync<Domain>(normalized);
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
