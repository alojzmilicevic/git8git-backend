using core.Auth;
using core.GitHub;
using core.GitHub.Models;
using core.Users;
using core.Users.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace extension_api.V1;

[Route("api/github")]
[Authorize]
public class GitHubController(
    IGitHubService gitHubService,
    IUsersStore usersStore,
    ICryptoService cryptoService,
    IJwtService jwtService)
    : ApiController
{
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> ExchangeToken([FromBody] TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequestResponse("Code is required");

        var accessToken = await gitHubService.ExchangeCodeForTokenAsync(request.Code);
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
            expiresIn = 3600
        });
    }

    [HttpGet("repos")]
    public async Task<IActionResult> ListRepos()
    {
        var token = await GetDecryptedAccessToken();
        if (token == null)
            return Unauthorized();

        var repos = await gitHubService.ListReposAsync(token);
        return Ok(repos);
    }

    [HttpGet("repos/{owner}/{repo}/branches")]
    public async Task<IActionResult> ListBranches(string owner, string repo)
    {
        var token = await GetDecryptedAccessToken();
        if (token == null)
            return Unauthorized();

        var branches = await gitHubService.ListBranchesAsync(token, owner, repo);
        return Ok(branches);
    }

    [HttpGet("repos/{owner}/{repo}/contents/{**path}")]
    public async Task<IActionResult> GetContents(string owner, string repo, string path, [FromQuery(Name = "ref")] string? gitRef)
    {
        var token = await GetDecryptedAccessToken();
        if (token == null)
            return Unauthorized();

        var content = await gitHubService.GetFileContentAsync(token, owner, repo, path, gitRef);
        if (content == null)
            return NotFoundResponse("File or directory not found");

        return Ok(content);
    }

    [HttpPut("repos/{owner}/{repo}/contents/{**path}")]
    public async Task<IActionResult> UpdateContents(
        string owner, string repo, string path,
        [FromBody] UpdateFileRequest request)
    {
        var token = await GetDecryptedAccessToken();
        if (token == null)
            return Unauthorized();

        var result = await gitHubService.CreateOrUpdateFileAsync(
            token, owner, repo, path,
            request.Content, request.Message, request.Branch, request.Sha);

        return Ok(result);
    }

    [HttpPost("repos/{owner}/{repo}/commits")]
    public async Task<IActionResult> CommitMultipleFiles(
        string owner, string repo,
        [FromBody] MultiFileCommitRequest request)
    {
        var token = await GetDecryptedAccessToken();
        if (token == null)
            return Unauthorized();

        if (request.Actions.Count == 0)
            return BadRequestResponse("At least one action is required");

        var result = await gitHubService.CommitMultipleFilesAsync(token, owner, repo, request);
        return Ok(result);
    }

    private async Task<string?> GetDecryptedAccessToken()
    {
        var userId = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return null;

        var user = await usersStore.FindByIdAsync(userId);
        if (user == null)
            return null;

        return cryptoService.Decrypt(user.AccessToken);
    }
}

public class TokenRequest
{
    public string Code { get; set; } = string.Empty;
}

public class UpdateFileRequest
{
    public string Content { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public string? Sha { get; set; }
}
