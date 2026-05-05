using System.Globalization;
using CloudAwesome.PipeLine.Contracts;
using CloudAwesome.PipeLine.Providers.GitHub.Client;
using CloudAwesome.PipeLine.Providers.GitHub.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudAwesome.PipeLine.Providers.GitHub;

public sealed class GitHubDashboardProvider : IPipelineDashboardProvider
{
    private readonly IGitHubClient _client;
    private readonly ILogger<GitHubDashboardProvider> _logger;
    private readonly IOptionsMonitor<GitHubOptions> _options;
    private readonly TimeProvider _timeProvider;

    public GitHubDashboardProvider(
        IGitHubClient client,
        IOptionsMonitor<GitHubOptions> options,
        TimeProvider timeProvider,
        ILogger<GitHubDashboardProvider> logger)
    {
        _client = client;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public PipelineProvider Provider => PipelineProvider.GitHub;

    public async Task<DashboardData> GetDashboardDataAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        var repositories = new List<DashboardRepository>();
        var pipelines = new List<DashboardPipeline>();
        var executions = new List<DashboardExecution>();

        foreach (var repositoryOptions in options.Repositories)
        {
            var repository = MapRepository(repositoryOptions);
            repositories.Add(repository);

            _logger.LogInformation("Querying GitHub repository {RepositoryId}.", repository.Id);
            var workflows = await _client.GetRepositoryWorkflowsAsync(
                repositoryOptions.Owner!,
                repositoryOptions.Name!,
                cancellationToken);

            foreach (var workflow in workflows.Where(IsActiveWorkflow))
            {
                var pipeline = MapPipeline(repository.Id, workflow, repositoryOptions);
                pipelines.Add(pipeline);

                var workflowRuns = await _client.GetWorkflowRunsAsync(
                    repositoryOptions.Owner!,
                    repositoryOptions.Name!,
                    workflow.Id,
                    cancellationToken);

                executions.AddRange(workflowRuns.Select(run => MapExecution(repository.Id, pipeline.Id, run)));
            }
        }

        return new DashboardData
        {
            GeneratedAt = _timeProvider.GetUtcNow(),
            Repositories = repositories,
            Pipelines = pipelines,
            Executions = executions
        };
    }

    private static DashboardRepository MapRepository(GitHubRepositoryOptions repository)
    {
        var id = CreateRepositoryId(repository.Owner!, repository.Name!);

        return new DashboardRepository
        {
            Id = id,
            Name = repository.Name!.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(repository.DisplayName) ? null : repository.DisplayName.Trim(),
            Url = $"https://github.com/{repository.Owner!.Trim()}/{repository.Name.Trim()}"
        };
    }

    private static DashboardPipeline MapPipeline(
        string repositoryId,
        GitHubWorkflow workflow,
        GitHubRepositoryOptions repository)
    {
        var mapping = repository.Pipelines.FirstOrDefault(
            pipeline => string.Equals(pipeline.Name, workflow.Name, StringComparison.OrdinalIgnoreCase));

        return new DashboardPipeline
        {
            Id = CreatePipelineId(repositoryId, workflow.Id),
            RepositoryId = repositoryId,
            Name = workflow.Name,
            Category = string.IsNullOrWhiteSpace(mapping?.Category)
                ? PipelineCategories.Other
                : mapping.Category.Trim(),
            Url = workflow.HtmlUrl
        };
    }

    private static DashboardExecution MapExecution(string repositoryId, string pipelineId, GitHubWorkflowRun run)
    {
        var status = NormalizeStatus(run.Status, run.Conclusion);
        var startedAt = run.RunStartedAt ?? run.CreatedAt;
        DateTimeOffset? completedAt = IsCompleted(run.Status) ? run.UpdatedAt : null;

        return new DashboardExecution
        {
            Id = run.Id.ToString(CultureInfo.InvariantCulture),
            RepositoryId = repositoryId,
            PipelineId = pipelineId,
            Status = status,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            DurationMs = completedAt is null ? null : Convert.ToInt64((completedAt.Value - startedAt).TotalMilliseconds),
            Branch = run.HeadBranch,
            CommitSha = run.HeadSha,
            Url = run.HtmlUrl
        };
    }

    private static string NormalizeStatus(string? status, string? conclusion)
    {
        if (IsCompleted(status))
        {
            return NormalizeConclusion(conclusion);
        }

        return status?.Trim().ToLowerInvariant() switch
        {
            "queued" or "requested" or "waiting" or "pending" => ExecutionStatuses.Queued,
            "in_progress" => ExecutionStatuses.Running,
            _ => ExecutionStatuses.Running
        };
    }

    private static string NormalizeConclusion(string? conclusion)
    {
        return conclusion?.Trim().ToLowerInvariant() switch
        {
            "success" => ExecutionStatuses.Success,
            "failure" or "timed_out" or "startup_failure" => ExecutionStatuses.Failure,
            "cancelled" => ExecutionStatuses.Cancelled,
            "neutral" or "skipped" or "action_required" => ExecutionStatuses.Neutral,
            _ => ExecutionStatuses.Neutral
        };
    }

    private static bool IsCompleted(string? status)
    {
        return string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsActiveWorkflow(GitHubWorkflow workflow)
    {
        return workflow.State is null || string.Equals(workflow.State, "active", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateRepositoryId(string owner, string repositoryName)
    {
        return $"{owner.Trim()}/{repositoryName.Trim()}".ToLowerInvariant();
    }

    private static string CreatePipelineId(string repositoryId, long workflowId)
    {
        return $"{repositoryId}/workflow/{workflowId.ToString(CultureInfo.InvariantCulture)}";
    }
}
