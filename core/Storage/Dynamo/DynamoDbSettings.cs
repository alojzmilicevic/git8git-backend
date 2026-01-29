namespace core.Storage.Dynamo;

public class DynamoDbSettings
{
    public DynamoDbEnvironment Environment { get; set; } = DynamoDbEnvironment.Local;
    public string LocalServiceUrl { get; set; } = "http://localhost:8000";
    public string Region { get; set; } = "eu-north-1";
    public string? Profile { get; set; }
}

public enum DynamoDbEnvironment
{
    Local,
    Development,
    Stage,
    Production
}
