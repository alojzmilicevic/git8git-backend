using Amazon.DynamoDBv2.DataModel;
using core.Storage.Dynamo.Converters;

namespace core.Licensing.Models;

[DynamoDBTable("Domains")]
public class Domain
{
    [DynamoDBHashKey]
    public string DomainName { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string LicenseKey { get; set; } = string.Empty;

    [DynamoDBProperty(typeof(EnumConverter<DomainStatus>))]
    public DomainStatus Status { get; set; } = DomainStatus.Active;

    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset? ExpiresAt { get; set; }
}

public enum DomainStatus
{
    Active,
    Expired,
    Disabled
}
