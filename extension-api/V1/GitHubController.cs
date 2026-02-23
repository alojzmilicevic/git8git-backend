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

    [HttpPost("repos/{owner}/{repo}/workflows/push")]
    public async Task<IActionResult> PushWorkflow(
        string owner, string repo,
        [FromBody] PushWorkflowRequest request)
    {
        var token = await GetDecryptedAccessToken();
        if (token == null)
            return Unauthorized();

        var result = await gitHubService.PushWorkflowAsync(token, owner, repo, request);
        return Ok(result);
    }

    [HttpGet("repos/{owner}/{repo}/workflows/{workflowId}")]
    public async Task<IActionResult> PullWorkflow(
        string owner, string repo, string workflowId,
        [FromQuery] string? branch)
    {
        var token = await GetDecryptedAccessToken();
        if (token == null)
            return Unauthorized();

        var result = await gitHubService.PullWorkflowAsync(token, owner, repo, workflowId, branch);
        if (!result.Found)
            return NotFoundResponse("Workflow not found in repository");

        return Ok(result);
    }

    [HttpDelete("repos/{owner}/{repo}/workflows/{workflowId}")]
    public async Task<IActionResult> DeleteWorkflow(
        string owner, string repo, string workflowId,
        [FromQuery] string branch, [FromQuery] string? workflowName)
    {
        var token = await GetDecryptedAccessToken();
        if (token == null)
            return Unauthorized();

        try
        {
            var result = await gitHubService.DeleteWorkflowAsync(token, owner, repo, workflowId, new DeleteWorkflowRequest
            {
                Branch = branch,
                WorkflowName = workflowName ?? ""
            });
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResponse(ex.Message);
        }
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

