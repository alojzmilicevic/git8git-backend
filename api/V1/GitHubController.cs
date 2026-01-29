using core.Auth;
using core.GitHub;
using core.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.V1;

[Route("api")]
[Authorize]
public class GitHubController : ApiController
{
    private readonly IGitHubService _gitHubService;
    private readonly IUsersStore _usersStore;
    private readonly ICryptoService _cryptoService;

    public GitHubController(
        IGitHubService gitHubService,
        IUsersStore usersStore,
        ICryptoService cryptoService)
    {
        _gitHubService = gitHubService;
        _usersStore = usersStore;
        _cryptoService = cryptoService;
    }

    [HttpGet("repos")]
    public async Task<IActionResult> ListRepos()
    {
        var accessToken = await GetDecryptedAccessToken();
        if (accessToken == null)
            return Unauthorized();

        var repos = await _gitHubService.ListReposAsync(accessToken);
        return Ok(repos);
    }

    [HttpGet("repos/{owner}/{repo}/branches")]
    public async Task<IActionResult> ListBranches(string owner, string repo)
    {
        var accessToken = await GetDecryptedAccessToken();
        if (accessToken == null)
            return Unauthorized();

        var branches = await _gitHubService.ListBranchesAsync(accessToken, owner, repo);
        return Ok(branches);
    }

    [HttpGet("repos/{owner}/{repo}/contents/{**path}")]
    public async Task<IActionResult> GetContents(string owner, string repo, string path, [FromQuery(Name = "ref")] string? gitRef)
    {
        var accessToken = await GetDecryptedAccessToken();
        if (accessToken == null)
            return Unauthorized();

        var content = await _gitHubService.GetFileContentAsync(accessToken, owner, repo, path, gitRef);
        if (content == null)
            return NotFound(new { error = "File or directory not found" });

        return Ok(content);
    }

    [HttpPut("repos/{owner}/{repo}/contents/{**path}")]
    public async Task<IActionResult> UpdateContents(
        string owner, string repo, string path,
        [FromBody] UpdateFileRequest request)
    {
        var accessToken = await GetDecryptedAccessToken();
        if (accessToken == null)
            return Unauthorized();

        var result = await _gitHubService.CreateOrUpdateFileAsync(
            accessToken, owner, repo, path,
            request.Content, request.Message, request.Branch, request.Sha);

        return Ok(result);
    }

    private async Task<string?> GetDecryptedAccessToken()
    {
        var userId = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return null;

        var user = await _usersStore.FindByIdAsync(userId);
        if (user == null)
            return null;

        return _cryptoService.Decrypt(user.AccessToken);
    }
}

public class UpdateFileRequest
{
    public string Content { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public string? Sha { get; set; }
}
