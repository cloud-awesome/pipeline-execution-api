using System.Text.Json.Serialization;

namespace CloudAwesome.PipeLine.Contracts;

public sealed record DashboardPipeline
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("repositoryId")]
    public required string RepositoryId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}
