using System.Text.Json.Serialization;

namespace CloudAwesome.PipeLine.Providers.GitHub.Client;

public sealed record GitHubWorkflowRun
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("workflow_id")]
    public required long WorkflowId { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("conclusion")]
    public string? Conclusion { get; init; }

    [JsonPropertyName("head_branch")]
    public string? HeadBranch { get; init; }

    [JsonPropertyName("head_sha")]
    public string? HeadSha { get; init; }

    [JsonPropertyName("run_started_at")]
    public DateTimeOffset? RunStartedAt { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; init; }
}
