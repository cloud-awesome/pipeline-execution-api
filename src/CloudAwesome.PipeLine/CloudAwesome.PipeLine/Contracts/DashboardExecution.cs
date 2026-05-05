using System.Text.Json.Serialization;

namespace CloudAwesome.PipeLine.Contracts;

public sealed record DashboardExecution
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("repositoryId")]
    public required string RepositoryId { get; init; }

    [JsonPropertyName("pipelineId")]
    public required string PipelineId { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("startedAt")]
    public required DateTimeOffset StartedAt { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; init; }

    [JsonPropertyName("branch")]
    public string? Branch { get; init; }

    [JsonPropertyName("commitSha")]
    public string? CommitSha { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}
