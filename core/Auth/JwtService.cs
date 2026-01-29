using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using core.Users.Models;
using Microsoft.IdentityModel.Tokens;

namespace core.Auth;

public abstract class JwtService : IJwtService
{
    private readonly AuthSettings _settings;
    private readonly SymmetricSecurityKey _signingKey;

    protected JwtService(AuthSettings settings)
    {
        _settings = settings;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.JwtSecret));
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim("userId", user.UserId),
            new Claim("username", user.Username)
        };

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_settings.JwtExpiryHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    public JwtPayload? ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey
            }, out _);

            var userId = principal.FindFirst("userId")?.Value;
            var username = principal.FindFirst("username")?.Value;

            if (userId == null || username == null)
                return null;

            return new JwtPayload { UserId = userId, Username = username };
        }
        catch
        {
            return null;
        }
    }
}
