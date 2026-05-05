using System.Text.Json.Serialization;

namespace CloudAwesome.PipeLine.Contracts;

public sealed record DashboardData
{
    [JsonPropertyName("generatedAt")]
    public required DateTimeOffset GeneratedAt { get; init; }

    [JsonPropertyName("repositories")]
    public required IReadOnlyList<DashboardRepository> Repositories { get; init; }

    [JsonPropertyName("pipelines")]
    public required IReadOnlyList<DashboardPipeline> Pipelines { get; init; }

    [JsonPropertyName("executions")]
    public required IReadOnlyList<DashboardExecution> Executions { get; init; }
}
