using core.Users.Models;

namespace core.Auth;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    JwtPayload? ValidateToken(string token);
}

public class JwtPayload
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
