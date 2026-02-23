namespace core.GitHub.Models;

public class PushWorkflowRequest
{
    public string Branch { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string WorkflowJson { get; set; } = string.Empty;
}

public class PushWorkflowResult
{
    public string? CommitSha { get; set; }
    public bool Changed { get; set; }
}

public class PullWorkflowResult
{
    public string? Content { get; set; }
    public bool Found { get; set; }
}

public class DeleteWorkflowRequest
{
    public string Branch { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
}

public class DeleteWorkflowResult
{
    public string CommitSha { get; set; } = string.Empty;
}
