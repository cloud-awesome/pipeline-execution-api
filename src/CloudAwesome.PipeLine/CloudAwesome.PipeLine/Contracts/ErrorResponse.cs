using System.Text.Json.Serialization;

namespace CloudAwesome.PipeLine.Contracts;

public sealed record ErrorResponse
{
    [JsonPropertyName("error")]
    public required string Error { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("details")]
    public IReadOnlyList<string> Details { get; init; } = [];

    [JsonPropertyName("retryAfter")]
    public DateTimeOffset? RetryAfter { get; init; }
}
