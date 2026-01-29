namespace core.GitHub.Models;

public class FileContentDto
{
    public string Type { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Sha { get; set; }
    public string? Path { get; set; }
    public List<DirectoryItemDto>? Items { get; set; }
}

public class DirectoryItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Sha { get; set; } = string.Empty;
}
