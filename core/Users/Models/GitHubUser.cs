namespace core.Users.Models;

public class GitHubUser
{
    public string Id { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
}