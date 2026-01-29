using System.Collections.Concurrent;
using System.Security.Cryptography;
using core.Auth;
using core.GitHub;
using core.Users;
using core.Users.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.V1;

[Route("auth")]
public class AuthController(
    IGitHubService gitHubService,
    IUsersStore usersStore,
    ICryptoService cryptoService,
    IJwtService jwtService)
    : ApiController
{
    private static readonly ConcurrentDictionary<string, DateTime> OAuthStates = new();

    [HttpGet("github")]
    public IActionResult InitiateGitHubOAuth()
    {
        CleanupExpiredStates();

        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        OAuthStates[state] = DateTime.UtcNow;

        var authUrl = gitHubService.GetAuthorizationUrl(state);
        return Redirect(authUrl);
    }

    [HttpGet("github/callback")]
    public async Task<IActionResult> GitHubCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            return Content(GenerateErrorHtml(error), "text/html");
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return Content(GenerateErrorHtml("Missing code or state"), "text/html");
        }

        if (!OAuthStates.TryRemove(state, out _))
        {
            return Content(GenerateErrorHtml("Invalid or expired state"), "text/html");
        }

        var accessToken = await gitHubService.ExchangeCodeForTokenAsync(code);
        var gitHubUser = await gitHubService.GetUserAsync(accessToken);

        var encryptedToken = cryptoService.Encrypt(accessToken);
        var refreshToken = jwtService.GenerateRefreshToken();

        var user = await usersStore.FindByIdAsync(gitHubUser.Id) ?? new User
        {
            UserId = gitHubUser.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.Username = gitHubUser.Login;
        user.Email = gitHubUser.Email;
        user.AvatarUrl = gitHubUser.AvatarUrl;
        user.AccessToken = encryptedToken;
        user.RefreshToken = refreshToken;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await usersStore.SaveAsync(user);

        var jwtToken = jwtService.GenerateAccessToken(user);
        const int expiresIn = 3600;

        return Content(GenerateSuccessHtml(jwtToken, refreshToken, expiresIn), "text/html");
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await usersStore.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { error = "User not found" });

        return Ok(new
        {
            userId = user.UserId,
            username = user.Username,
            email = user.Email,
            avatarUrl = user.AvatarUrl
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            return BadRequest(new { error = "Refresh token required" });

        var user = await usersStore.FindByRefreshTokenAsync(request.RefreshToken);
        if (user == null)
            return Unauthorized(new { error = "Invalid refresh token" });

        var newAccessToken = jwtService.GenerateAccessToken(user);
        var newRefreshToken = jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await usersStore.SaveAsync(user);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken,
            expiresIn = 3600
        });
    }

    private static void CleanupExpiredStates()
    {
        var expiry = DateTime.UtcNow.AddMinutes(-10);
        var expiredKeys = OAuthStates.Where(kvp => kvp.Value < expiry).Select(kvp => kvp.Key).ToList();
        foreach (var key in expiredKeys)
        {
            OAuthStates.TryRemove(key, out _);
        }
    }

    private static string GenerateSuccessHtml(string accessToken, string refreshToken, int expiresIn)
    {
        return @"<!DOCTYPE html>
<html>
<head>
    <title>Authentication Successful</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background: #f5f5f5; }
        .container { text-align: center; padding: 40px; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #28a745; margin-bottom: 16px; }
        p { color: #666; }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>Authentication Successful</h1>
        <p>You can close this window.</p>
    </div>
    <script>
        const data = {
            type: 'GITHUB_AUTH_SUCCESS',
            accessToken: '" + accessToken + @"',
            refreshToken: '" + refreshToken + @"',
            expiresIn: " + expiresIn + @"
        };
        if (window.opener) {
            window.opener.postMessage(data, '*');
        }
        window.location.hash = 'accessToken=" + accessToken + "&refreshToken=" + refreshToken + "&expiresIn=" + expiresIn + @"';
    </script>
</body>
</html>";
    }

    private static string GenerateErrorHtml(string error)
    {
        return @"<!DOCTYPE html>
<html>
<head>
    <title>Authentication Failed</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background: #f5f5f5; }
        .container { text-align: center; padding: 40px; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #dc3545; margin-bottom: 16px; }
        p { color: #666; }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>Authentication Failed</h1>
        <p>" + error + @"</p>
    </div>
</body>
</html>";
    }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
