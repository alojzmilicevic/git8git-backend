using core.Auth;
using core.GitHub;
using core.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.V1;

[Route("api/repos")]
[Authorize]
public class GitHubController(
    IGitHubService gitHubService,
    IUsersStore usersStore,
    ICryptoService cryptoService)
    : ApiController
{
    [HttpGet("")]
    public async Task<IActionResult> ListRepos()
    {
        var accessToken = await GetDecryptedAccessToken();
        if (accessToken == null)
            return Unauthorized();

        var repos = await gitHubService.ListReposAsync(accessToken);
        return Ok(repos);
    }

    [HttpGet("{owner}/{repo}/branches")]
    public async Task<IActionResult> ListBranches(string owner, string repo)
    {
        var accessToken = await GetDecryptedAccessToken();
        if (accessToken == null)
            return Unauthorized();

        var branches = await gitHubService.ListBranchesAsync(accessToken, owner, repo);
        return Ok(branches);
    }

    [HttpGet("{owner}/{repo}/contents/{**path}")]
    public async Task<IActionResult> GetContents(string owner, string repo, string path, [FromQuery(Name = "ref")] string? gitRef)
    {
        var accessToken = await GetDecryptedAccessToken();
        if (accessToken == null)
            return Unauthorized();

        var content = await gitHubService.GetFileContentAsync(accessToken, owner, repo, path, gitRef);
        if (content == null)
            return NotFound(new { error = "File or directory not found" });

        return Ok(content);
    }

    [HttpPut("{owner}/{repo}/contents/{**path}")]
    public async Task<IActionResult> UpdateContents(
        string owner, string repo, string path,
        [FromBody] UpdateFileRequest request)
    {
        var accessToken = await GetDecryptedAccessToken();
        if (accessToken == null)
            return Unauthorized();

        var result = await gitHubService.CreateOrUpdateFileAsync(
            accessToken, owner, repo, path,
            request.Content, request.Message, request.Branch, request.Sha);

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

public class UpdateFileRequest
{
    public string Content { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public string? Sha { get; set; }
}
