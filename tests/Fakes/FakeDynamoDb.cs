using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using core.Storage.Dynamo;
using FakeItEasy;

namespace tests.Fakes;

public class FakeDynamoDb : IDynamoDb
{
    public FakeDynamoDb()
    {
        Context = A.Fake<IDynamoDBContext>();
        Client = A.Fake<AmazonDynamoDBClient>();
    }

    public IDynamoDBContext Context { get; }
    public AmazonDynamoDBClient Client { get; }

    public IMockableBatchWrite<T> CreateBatchWrite<T>()
    {
        return new FakeBatchWrite<T>();
    }

    public IMockableBatchGet<T> CreateBatchGet<T>()
    {
        return new FakeBatchGet<T>();
    }

    public void WarmUp()
    {
    }
}

public class FakeBatchWrite<T> : IMockableBatchWrite<T>
{
    private readonly List<T> _itemsToAdd = new();
    private readonly List<object> _keysToDelete = new();

    public void AddPutItem(T item) => _itemsToAdd.Add(item);
    public void AddDeleteKey(object hashKey) => _keysToDelete.Add(hashKey);
    public void AddDeleteKey(object hashKey, object rangeKey) => _keysToDelete.Add((hashKey, rangeKey));
    public Task ExecuteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public IReadOnlyList<T> ItemsToAdd => _itemsToAdd;
    public IReadOnlyList<object> KeysToDelete => _keysToDelete;
}

public class FakeBatchGet<T> : IMockableBatchGet<T>
{
    private readonly List<object> _keys = new();
    private readonly List<T> _results = new();

    public void AddKey(object hashKey) => _keys.Add(hashKey);
    public void AddKey(object hashKey, object rangeKey) => _keys.Add((hashKey, rangeKey));
    public Task ExecuteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public List<T> Results => _results;

    public void SetResults(params T[] items)
    {
        _results.Clear();
        _results.AddRange(items);
    }
}
