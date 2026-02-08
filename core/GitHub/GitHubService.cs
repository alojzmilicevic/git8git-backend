using System.Text;
using core.GitHub.Models;
using core.Users.Models;
using Octokit;

namespace core.GitHub;

public class GitHubService(GitHubSettings settings) : IGitHubService
{
    private const string Scopes = "repo,read:user,user:email";

    public string GetAuthorizationUrl(string state)
    {
        var request = new OauthLoginRequest(settings.ClientId)
        {
            Scopes = { "repo", "read:user", "user:email" },
            State = state,
            RedirectUri = new Uri(settings.CallbackUrl)
        };

        var client = new GitHubClient(new ProductHeaderValue("Git8Git"));
        return client.Oauth.GetGitHubLoginUrl(request).ToString();
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code)
    {
        var client = new GitHubClient(new ProductHeaderValue("Git8Git"));
        var request = new OauthTokenRequest(settings.ClientId, settings.ClientSecret, code);
        var token = await client.Oauth.CreateAccessToken(request);
        return token.AccessToken;
    }

    public async Task<GitHubUser> GetUserAsync(string accessToken)
    {
        var client = CreateClient(accessToken);
        var user = await client.User.Current();
        
        string? email = user.Email;
        if (string.IsNullOrEmpty(email))
        {
            var emails = await client.User.Email.GetAll();
            email = emails.FirstOrDefault(e => e.Primary)?.Email ?? emails.FirstOrDefault()?.Email;
        }

        return new GitHubUser
        {
            Id = user.Id.ToString(),
            Login = user.Login,
            Email = email,
            AvatarUrl = user.AvatarUrl
        };
    }

    public async Task<IEnumerable<RepoDto>> ListReposAsync(string accessToken)
    {
        var client = CreateClient(accessToken);
        var repos = await client.Repository.GetAllForCurrent(new RepositoryRequest
        {
            Sort = RepositorySort.Updated,
            Direction = SortDirection.Descending
        });

        return repos.Select(r => new RepoDto
        {
            Id = r.Id,
            Name = r.Name,
            FullName = r.FullName,
            Owner = r.Owner.Login,
            Private = r.Private,
            DefaultBranch = r.DefaultBranch ?? "main"
        });
    }

    public async Task<IEnumerable<BranchDto>> ListBranchesAsync(string accessToken, string owner, string repo)
    {
        var client = CreateClient(accessToken);
        var branches = await client.Repository.Branch.GetAll(owner, repo);

        return branches.Select(b => new BranchDto
        {
            Name = b.Name,
            Protected = b.Protected
        });
    }

    public async Task<FileContentDto?> GetFileContentAsync(string accessToken, string owner, string repo, string path, string? gitRef)
    {
        var client = CreateClient(accessToken);

        try
        {
            var contents = string.IsNullOrEmpty(gitRef)
                ? await client.Repository.Content.GetAllContents(owner, repo, path)
                : await client.Repository.Content.GetAllContentsByRef(owner, repo, path, gitRef);

            if (contents.Count == 0)
                return null;

            if (contents.Count > 1 || contents[0].Type == ContentType.Dir)
            {
                return new FileContentDto
                {
                    Type = "directory",
                    Items = contents.Select(c => new DirectoryItemDto
                    {
                        Name = c.Name,
                        Path = c.Path,
                        Type = c.Type.StringValue,
                        Sha = c.Sha
                    }).ToList()
                };
            }

            var file = contents[0];
            var content = file.Content;
            
            if (!string.IsNullOrEmpty(file.EncodedContent))
            {
                content = Encoding.UTF8.GetString(Convert.FromBase64String(file.EncodedContent));
            }

            return new FileContentDto
            {
                Type = "file",
                Content = content,
                Sha = file.Sha,
                Path = file.Path
            };
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    public async Task<FileUpdateResultDto> CreateOrUpdateFileAsync(
        string accessToken, string owner, string repo, string path,
        string content, string message, string? branch, string? sha)
    {
        var client = CreateClient(accessToken);

        if (!string.IsNullOrEmpty(sha))
        {
            try
            {
                var existing = await GetFileContentAsync(accessToken, owner, repo, path, branch);
                if (existing?.Content == content)
                {
                    return new FileUpdateResultDto
                    {
                        Changed = false,
                        Sha = sha,
                        Path = path
                    };
                }
            }
            catch { }
        }

        RepositoryContentChangeSet result;

        if (string.IsNullOrEmpty(sha))
        {
            var request = new CreateFileRequest(message, content);
            if (!string.IsNullOrEmpty(branch))
                request.Branch = branch;

            result = await client.Repository.Content.CreateFile(owner, repo, path, request);
        }
        else
        {
            var request = new UpdateFileRequest(message, content, sha);
            if (!string.IsNullOrEmpty(branch))
                request.Branch = branch;

            result = await client.Repository.Content.UpdateFile(owner, repo, path, request);
        }

        return new FileUpdateResultDto
        {
            Changed = true,
            Sha = result.Content.Sha,
            Path = result.Content.Path
        };
    }

    private static GitHubClient CreateClient(string accessToken)
    {
        var client = new GitHubClient(new ProductHeaderValue("Git8Git"))
        {
            Credentials = new Credentials(accessToken)
        };
        return client;
    }
}
