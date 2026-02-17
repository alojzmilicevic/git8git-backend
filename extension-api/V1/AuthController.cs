using core.Auth;
using core.GitHub;
using core.Users;
using core.Users.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace extension_api.V1;

[Route("auth")]
public class AuthController(
    IUsersStore usersStore,
    IJwtService jwtService,
    IGitHubService gitHubService,
    ICryptoService cryptoService,
    IWebHostEnvironment env)
    : ApiController
{
    [HttpGet("github")]
    [AllowAnonymous]
    public IActionResult GitHubLogin()
    {
        if (!env.IsDevelopment())
            return NotFound();

        var state = Guid.NewGuid().ToString("N");
        var callbackUrl = $"{Request.Scheme}://{Request.Host}/auth/github/callback";
        var url = gitHubService.GetAuthorizationUrl(state, callbackUrl);
        return Redirect(url);
    }

    [HttpGet("github/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GitHubCallback([FromQuery] string code)
    {
        if (!env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(code))
            return BadRequestResponse("Code is required");

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

        return Ok(new
        {
            accessToken = jwtToken,
            refreshToken,
            expiresIn = 3600,
            usage = "Copy the accessToken and use it in Swagger's Authorize button as: Bearer <accessToken>"
        });
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
            return NotFoundResponse("User not found");

        return Ok(new
        {
            userId = user.UserId,
            username = user.Username,
            email = user.Email,
            avatarUrl = user.AvatarUrl
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequestResponse("Refresh token is required");

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
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
