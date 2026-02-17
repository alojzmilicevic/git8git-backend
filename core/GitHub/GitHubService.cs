using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using core.GitHub.Models;
using core.Users.Models;
using Octokit;

namespace core.GitHub;

public interface IGitHubService
{
    string GetAuthorizationUrl(string state, string? redirectUri = null);
    Task<string> ExchangeCodeForTokenAsync(string code);
    Task<GitHubUser> GetUserAsync(string accessToken);
    Task<IEnumerable<RepoDto>> ListReposAsync(string accessToken);
    Task<IEnumerable<BranchDto>> ListBranchesAsync(string accessToken, string owner, string repo);
    Task<FileContentDto?> GetFileContentAsync(string accessToken, string owner, string repo, string path, string? gitRef);
    Task<FileUpdateResultDto> CreateOrUpdateFileAsync(string accessToken, string owner, string repo, string path, string content, string message, string? branch, string? sha);
    Task<MultiFileCommitResult> CommitMultipleFilesAsync(string accessToken, string owner, string repo, MultiFileCommitRequest request);
}

public class GitHubService(GitHubSettings settings) : IGitHubService
{
    private const string Scopes = "repo,read:user,user:email";

    public string GetAuthorizationUrl(string state, string? redirectUri = null)
    {
        var request = new OauthLoginRequest(settings.ClientId)
        {
            Scopes = { "repo", "read:user", "user:email" },
            State = state,
            RedirectUri = new Uri(redirectUri ?? settings.CallbackUrl)
        };

        var client = new GitHubClient(new Octokit.ProductHeaderValue("Git8Git"));
        return client.Oauth.GetGitHubLoginUrl(request).ToString();
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code)
    {
        var client = new GitHubClient(new Octokit.ProductHeaderValue("Git8Git"));
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

    public async Task<MultiFileCommitResult> CommitMultipleFilesAsync(
        string accessToken,
        string owner,
        string repo,
        MultiFileCommitRequest request)
    {
        try
        {
            var client = CreateClient(accessToken);
            var branch = request.Branch;

            if (string.IsNullOrEmpty(branch))
            {
                var repository = await client.Repository.Get(owner, repo);
                branch = repository.DefaultBranch;
            }

            // Get the latest commit on the branch
            var reference = await client.Git.Reference.Get(owner, repo, $"heads/{branch}");
            var baseCommitSha = reference.Object.Sha;

            var baseCommit = await client.Git.Commit.Get(owner, repo, baseCommitSha);
            var baseTreeSha = baseCommit.Tree.Sha;

            // Build a new tree with all the file actions
            var newTree = new NewTree { BaseTree = baseTreeSha };

            foreach (var action in request.Actions)
            {
                if (action.Action == "delete")
                {
                    newTree.Tree.Add(new NewTreeItem
                    {
                        Path = action.Path,
                        Mode = "100644",
                        Type = TreeType.Blob,
                        Sha = null // null SHA = delete
                    });
                }
                else
                {
                    // create_or_update: create a blob first
                    var blob = new NewBlob
                    {
                        Content = action.Content,
                        Encoding = EncodingType.Utf8
                    };

                    var blobResult = await client.Git.Blob.Create(owner, repo, blob);

                    newTree.Tree.Add(new NewTreeItem
                    {
                        Path = action.Path,
                        Mode = "100644",
                        Type = TreeType.Blob,
                        Sha = blobResult.Sha
                    });
                }
            }

            // Create the new tree via raw HTTP so that "sha": null for deletes is preserved
            var treeSha = await CreateTreeWithHttpAsync(accessToken, owner, repo, baseTreeSha, newTree);

            // Create the commit
            var newCommit = new NewCommit(request.Message, treeSha, baseCommitSha);
            var commitResult = await client.Git.Commit.Create(owner, repo, newCommit);

            // Update the branch reference
            await client.Git.Reference.Update(
                owner,
                repo,
                $"heads/{branch}",
                new ReferenceUpdate(commitResult.Sha)
            );

            return new MultiFileCommitResult
            {
                CommitSha = commitResult.Sha
            };
        }
        catch (Octokit.RateLimitExceededException ex)
        {
            throw new Exception(
                $"GitHub rate limit exceeded. Resets at {ex.Reset}.",
                ex);
        }
        catch (Octokit.ApiValidationException ex)
        {
            throw new Exception(
                $"GitHub validation failed: {string.Join(", ", ex.ApiError.Errors.Select(e => e.Message))}",
                ex);
        }
        catch (Octokit.ApiException ex)
        {
            throw new Exception(
                $"GitHub API error ({ex.StatusCode}): {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Unexpected error committing files: {ex.Message}",
                ex);
        }
    }


    private static GitHubClient CreateClient(string accessToken)
    {
        var client = new GitHubClient(new Octokit.ProductHeaderValue("Git8Git"))
        {
            Credentials = new Credentials(accessToken)
        };
        return client;
    }

    private static async Task<string> CreateTreeWithHttpAsync(
        string accessToken,
        string owner,
        string repo,
        string baseTreeSha,
        NewTree newTree)
    {
        using var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Git8Git");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

        var requestBody = new
        {
            base_tree = baseTreeSha,
            tree = newTree.Tree.Select(item => new
            {
                path = item.Path,
                mode = item.Mode,
                type = "blob",
                sha = item.Sha
            })
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"https://api.github.com/repos/{owner}/{repo}/git/trees";
        using var response = await httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(responseStream);
        var sha = doc.RootElement.GetProperty("sha").GetString();

        if (string.IsNullOrEmpty(sha))
            throw new InvalidOperationException("GitHub tree response did not contain a SHA.");

        return sha;
    }
}
