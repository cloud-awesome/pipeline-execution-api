namespace CloudAwesome.PipeLine.Providers.GitHub.Client;

public interface IGitHubClient
{
    Task<IReadOnlyList<GitHubWorkflow>> GetRepositoryWorkflowsAsync(
        string owner,
        string repositoryName,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GitHubWorkflowRun>> GetWorkflowRunsAsync(
        string owner,
        string repositoryName,
        long workflowId,
        CancellationToken cancellationToken);
}
