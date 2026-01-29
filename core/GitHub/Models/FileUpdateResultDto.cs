namespace core.GitHub.Models;

public class FileUpdateResultDto
{
    public bool Changed { get; set; }
    public string Sha { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}
