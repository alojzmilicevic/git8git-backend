using core.Storage.Dynamo;
using core.Users.Models;

namespace core.Users;

public abstract class UsersStore(IDynamoDb dynamoDb) : IUsersStore
{
    public async Task<User?> FindByIdAsync(string userId)
    {
        return await dynamoDb.Context.LoadAsync<User>(userId);
    }

    public async Task<User?> FindByRefreshTokenAsync(string refreshToken)
    {
        var search = dynamoDb.Context.ScanAsync<User>(
            [new Amazon.DynamoDBv2.DataModel.ScanCondition("RefreshToken", Amazon.DynamoDBv2.DocumentModel.ScanOperator.Equal, refreshToken)
            ]);
        var results = await search.GetRemainingAsync();
        return results.FirstOrDefault();
    }

    public async Task SaveAsync(User user)
    {
        await dynamoDb.Context.SaveAsync(user);
    }

    public async Task DeleteAsync(string userId)
    {
        await dynamoDb.Context.DeleteAsync<User>(userId);
    }
}
