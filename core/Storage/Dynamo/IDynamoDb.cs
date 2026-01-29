using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace core.Storage.Dynamo;

public interface IDynamoDb
{
    IDynamoDBContext Context { get; }
    AmazonDynamoDBClient Client { get; }
    IMockableBatchWrite<T> CreateBatchWrite<T>();
    IMockableBatchGet<T> CreateBatchGet<T>();
    void WarmUp();
}

public interface IMockableBatchWrite<T>
{
    void AddPutItem(T item);
    void AddDeleteKey(object hashKey);
    void AddDeleteKey(object hashKey, object rangeKey);
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

public interface IMockableBatchGet<T>
{
    void AddKey(object hashKey);
    void AddKey(object hashKey, object rangeKey);
    Task ExecuteAsync(CancellationToken cancellationToken = default);
    List<T> Results { get; }
}
