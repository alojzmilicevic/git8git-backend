using core.Storage.Dynamo;
using core.Users.Models;

namespace core.Users;

public class UsersStore : IUsersStore
{
    private readonly IDynamoDb _dynamoDb;

    public UsersStore(IDynamoDb dynamoDb)
    {
        _dynamoDb = dynamoDb;
    }

    public async Task<User?> FindByIdAsync(string userId)
    {
        return await _dynamoDb.Context.LoadAsync<User>(userId);
    }

    public async Task<User?> FindByRefreshTokenAsync(string refreshToken)
    {
        var search = _dynamoDb.Context.ScanAsync<User>(
            new[] { new Amazon.DynamoDBv2.DataModel.ScanCondition("RefreshToken", Amazon.DynamoDBv2.DocumentModel.ScanOperator.Equal, refreshToken) });
        var results = await search.GetRemainingAsync();
        return results.FirstOrDefault();
    }

    public async Task SaveAsync(User user)
    {
        await _dynamoDb.Context.SaveAsync(user);
    }

    public async Task DeleteAsync(string userId)
    {
        await _dynamoDb.Context.DeleteAsync<User>(userId);
    }
}
