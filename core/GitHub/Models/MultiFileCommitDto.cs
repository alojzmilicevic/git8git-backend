namespace core.GitHub.Models;

public class MultiFileCommitRequest
{
    public string Message { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public List<FileAction> Actions { get; set; } = [];
}

public class FileAction
{
    /// <summary>
    /// "create_or_update" or "delete"
    /// </summary>
    public string Action { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    /// <summary>
    /// Required for create_or_update, ignored for delete.
    /// </summary>
    public string? Content { get; set; }
}

public class MultiFileCommitResult
{
    public string CommitSha { get; set; } = string.Empty;
}
