using core.Users.Models;

namespace core.Users;

public interface IUsersStore
{
    Task<User?> FindByIdAsync(string userId);
    Task<User?> FindByRefreshTokenAsync(string refreshToken);
    Task SaveAsync(User user);
    Task DeleteAsync(string userId);
}
