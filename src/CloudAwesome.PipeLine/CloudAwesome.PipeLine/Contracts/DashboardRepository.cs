using System.Text.Json.Serialization;

namespace CloudAwesome.PipeLine.Contracts;

public sealed record DashboardRepository
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}
