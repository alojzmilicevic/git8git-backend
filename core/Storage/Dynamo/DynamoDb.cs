using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace core.Storage.Dynamo;

public class DynamoDb : IDynamoDb
{
    private readonly DynamoDbSettings _settings;

    public DynamoDb(DynamoDbSettings settings)
    {
        _settings = settings;

        if (_settings.Environment == DynamoDbEnvironment.Local)
        {
            var config = new AmazonDynamoDBConfig
            {
                ServiceURL = _settings.LocalServiceUrl
            };
            Client = new AmazonDynamoDBClient(new BasicAWSCredentials("local", "local"), config);
        }
        else
        {
            var region = RegionEndpoint.GetBySystemName(_settings.Region);
            
            if (!string.IsNullOrEmpty(_settings.Profile))
            {
                var chain = new CredentialProfileStoreChain();
                if (chain.TryGetAWSCredentials(_settings.Profile, out var credentials))
                {
                    Client = new AmazonDynamoDBClient(credentials, region);
                }
                else
                {
                    throw new Exception($"Failed to load AWS profile '{_settings.Profile}'");
                }
            }
            else
            {
                Client = new AmazonDynamoDBClient(region);
            }
        }

        var contextConfig = new DynamoDBContextConfig
        {
            TableNamePrefix = string.Empty
        };

        Context = new DynamoDBContext(Client, contextConfig);
    }

    public IDynamoDBContext Context { get; }
    public AmazonDynamoDBClient Client { get; }

    public IMockableBatchWrite<T> CreateBatchWrite<T>()
    {
        return new MockableBatchWrite<T>(Context.CreateBatchWrite<T>());
    }

    public IMockableBatchGet<T> CreateBatchGet<T>()
    {
        return new MockableBatchGet<T>(Context.CreateBatchGet<T>());
    }

    public void WarmUp()
    {
    }
}

public class MockableBatchWrite<T> : IMockableBatchWrite<T>
{
    private readonly IBatchWrite<T> _batchWrite;

    public MockableBatchWrite(IBatchWrite<T> batchWrite)
    {
        _batchWrite = batchWrite;
    }

    public void AddPutItem(T item) => _batchWrite.AddPutItem(item);
    public void AddDeleteKey(object hashKey) => _batchWrite.AddDeleteKey(hashKey);
    public void AddDeleteKey(object hashKey, object rangeKey) => _batchWrite.AddDeleteKey(hashKey, rangeKey);
    public Task ExecuteAsync(CancellationToken cancellationToken = default) => _batchWrite.ExecuteAsync(cancellationToken);
}

public class MockableBatchGet<T> : IMockableBatchGet<T>
{
    private readonly IBatchGet<T> _batchGet;

    public MockableBatchGet(IBatchGet<T> batchGet)
    {
        _batchGet = batchGet;
    }

    public void AddKey(object hashKey) => _batchGet.AddKey(hashKey);
    public void AddKey(object hashKey, object rangeKey) => _batchGet.AddKey(hashKey, rangeKey);
    public Task ExecuteAsync(CancellationToken cancellationToken = default) => _batchGet.ExecuteAsync(cancellationToken);
    public List<T> Results => _batchGet.Results;
}
