using System.Net;

namespace CloudAwesome.PipeLine.Providers.GitHub.Client;

public sealed class GitHubRateLimitExceededException : GitHubRequestFailedException
{
    public GitHubRateLimitExceededException(HttpStatusCode statusCode, string requestPath, DateTimeOffset? resetAt)
        : base(statusCode, requestPath)
    {
        ResetAt = resetAt;
    }

    public DateTimeOffset? ResetAt { get; }
}
