namespace core.GitHub;

public class GitHubFeature : Feature
{
    public GitHubFeature()
    {
        AddSettings<GitHubSettings>();
        AddDependency<IGitHubService, GitHubService>();
    }
}
