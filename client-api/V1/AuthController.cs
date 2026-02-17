using System.Security.Cryptography;
using System.Text;
using core.Auth;
using core.Users;
using core.Users.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace client_api.V1;

[Route("auth")]
public class AuthController(
    IUsersStore usersStore,
    IJwtService jwtService) : ApiController
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequestResponse("Email and password are required");

        var existing = await usersStore.FindByIdAsync(NormalizeEmail(request.Email));
        if (existing != null)
            return BadRequestResponse("An account with this email already exists");

        var passwordHash = HashPassword(request.Password);

        var user = new User
        {
            UserId = NormalizeEmail(request.Email),
            Username = request.Email,
            PasswordHash = passwordHash
        };

        await usersStore.SaveAsync(user);

        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        await usersStore.SaveAsync(user);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequestResponse("Email and password are required");

        var user = await usersStore.FindByIdAsync(NormalizeEmail(request.Email));
        if (user == null)
            return Unauthorized(new { error = "Invalid email or password" });

        if (string.IsNullOrEmpty(user.PasswordHash) || !VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid email or password" });

        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        await usersStore.SaveAsync(user);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequestResponse("Refresh token is required");

        var user = await usersStore.FindByRefreshTokenAsync(request.RefreshToken);
        if (user == null)
            return Unauthorized(new { error = "Invalid refresh token" });

        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        await usersStore.SaveAsync(user);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst("userId")?.Value;
        if (userId == null)
            return Unauthorized();

        var user = await usersStore.FindByIdAsync(userId);
        if (user == null)
            return NotFoundResponse("User not found");

        return Ok(new
        {
            userId = user.UserId,
            email = user.Username
        });
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return $"{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;

        var salt = Convert.FromHexString(parts[0]);
        var hash = Convert.FromHexString(parts[1]);

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return CryptographicOperations.FixedTimeEquals(hash, computedHash);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
