namespace core.Storage.Dynamo;

public class DynamoFeature : Feature
{
    public DynamoFeature()
    {
        AddSettings<DynamoDbSettings>();
    }

    public override Task InitAsync(IServiceProvider provider)
    {
        var dynamoDb = provider.GetRequiredService<IDynamoDb>();
        dynamoDb.WarmUp();
        return Task.CompletedTask;
    }
}

file static class ServiceProviderExtensions
{
    public static T GetRequiredService<T>(this IServiceProvider provider) where T : notnull
    {
        return (T)provider.GetService(typeof(T))!;
    }
}
