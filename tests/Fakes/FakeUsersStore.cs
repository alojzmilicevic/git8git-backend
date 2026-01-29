using System.Collections.Concurrent;
using core.Users;
using core.Users.Models;

namespace tests.Fakes;

public class FakeUsersStore : IUsersStore
{
    private readonly ConcurrentDictionary<string, User> _users = new();

    public Task<User?> FindByIdAsync(string userId)
    {
        _users.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> FindByRefreshTokenAsync(string refreshToken)
    {
        var user = _users.Values.FirstOrDefault(u => u.RefreshToken == refreshToken);
        return Task.FromResult(user);
    }

    public Task SaveAsync(User user)
    {
        _users[user.UserId] = user;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string userId)
    {
        _users.TryRemove(userId, out _);
        return Task.CompletedTask;
    }

    public void Clear() => _users.Clear();

    public void Seed(params User[] users)
    {
        foreach (var user in users)
        {
            _users[user.UserId] = user;
        }
    }

    public int Count => _users.Count;
}
