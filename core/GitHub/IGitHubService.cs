using core.GitHub.Models;
using core.Users.Models;

namespace core.GitHub;

public interface IGitHubService
{
    string GetAuthorizationUrl(string state);
    Task<string> ExchangeCodeForTokenAsync(string code);
    Task<GitHubUser> GetUserAsync(string accessToken);
    Task<IEnumerable<RepoDto>> ListReposAsync(string accessToken);
    Task<IEnumerable<BranchDto>> ListBranchesAsync(string accessToken, string owner, string repo);
    Task<FileContentDto?> GetFileContentAsync(string accessToken, string owner, string repo, string path, string? gitRef);
    Task<FileUpdateResultDto> CreateOrUpdateFileAsync(string accessToken, string owner, string repo, string path, string content, string message, string? branch, string? sha);
}
