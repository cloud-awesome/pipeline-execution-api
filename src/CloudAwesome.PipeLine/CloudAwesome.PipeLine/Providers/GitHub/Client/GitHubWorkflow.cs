using System.Text.Json.Serialization;

namespace CloudAwesome.PipeLine.Providers.GitHub.Client;

public sealed record GitHubWorkflow
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; init; }
}
