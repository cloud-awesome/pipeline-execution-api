using System.Net;

namespace CloudAwesome.PipeLine.Providers.GitHub.Client;

public class GitHubRequestFailedException : Exception
{
    public GitHubRequestFailedException(HttpStatusCode statusCode, string requestPath)
        : base($"GitHub request failed with status {(int)statusCode} ({statusCode}) for '{requestPath}'.")
    {
        StatusCode = statusCode;
        RequestPath = requestPath;
    }

    public HttpStatusCode StatusCode { get; }

    public string RequestPath { get; }
}
