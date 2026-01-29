namespace core.GitHub.Models;

public class RepoDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public bool Private { get; set; }
    public string DefaultBranch { get; set; } = string.Empty;
}
