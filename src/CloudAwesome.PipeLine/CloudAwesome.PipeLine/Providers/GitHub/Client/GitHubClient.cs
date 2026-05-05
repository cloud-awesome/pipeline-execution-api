using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using CloudAwesome.PipeLine.Providers.GitHub.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudAwesome.PipeLine.Providers.GitHub.Client;

public sealed class GitHubClient : IGitHubClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubClient> _logger;
    private readonly IOptionsMonitor<GitHubOptions> _options;

    public GitHubClient(
        HttpClient httpClient,
        IOptionsMonitor<GitHubOptions> options,
        ILogger<GitHubClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public Task<IReadOnlyList<GitHubWorkflow>> GetRepositoryWorkflowsAsync(
        string owner,
        string repositoryName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryName);

        var path = $"repos/{Encode(owner)}/{Encode(repositoryName)}/actions/workflows";

        return GetPagedAsync<GitHubWorkflowsResponse, GitHubWorkflow>(
            path,
            response => response.Workflows,
            cancellationToken);
    }

    public Task<IReadOnlyList<GitHubWorkflowRun>> GetWorkflowRunsAsync(
        string owner,
        string repositoryName,
        long workflowId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryName);

        if (workflowId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workflowId), workflowId, "Workflow id must be greater than zero.");
        }

        var path = $"repos/{Encode(owner)}/{Encode(repositoryName)}/actions/workflows/{workflowId.ToString(CultureInfo.InvariantCulture)}/runs";

        return GetPagedAsync<GitHubWorkflowRunsResponse, GitHubWorkflowRun>(
            path,
            response => response.WorkflowRuns,
            cancellationToken);
    }

    private async Task<IReadOnlyList<TItem>> GetPagedAsync<TResponse, TItem>(
        string path,
        Func<TResponse, IReadOnlyList<TItem>> getItems,
        CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        var results = new List<TItem>();
        var nextRequest = BuildInitialPagePath(path, options.PageSize);

        while (!string.IsNullOrWhiteSpace(nextRequest))
        {
            using var response = await SendWithRetriesAsync(nextRequest, cancellationToken);
            var payload = await ReadResponseAsync<TResponse>(response, nextRequest, cancellationToken);

            results.AddRange(getItems(payload));
            nextRequest = GetNextPagePath(response);
        }

        return results;
    }

    private async Task<HttpResponseMessage> SendWithRetriesAsync(string requestPath, CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;

        for (var attempt = 0; attempt <= options.MaxRetryAttempts; attempt++)
        {
            using var request = CreateRequest(requestPath, options);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (IsRateLimitResponse(response))
            {
                var statusCode = response.StatusCode;
                var resetAt = GetRateLimitReset(response);
                response.Dispose();
                throw new GitHubRateLimitExceededException(statusCode, requestPath, resetAt);
            }

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if (!IsTransient(response.StatusCode) || attempt == options.MaxRetryAttempts)
            {
                var statusCode = response.StatusCode;
                response.Dispose();
                throw new GitHubRequestFailedException(statusCode, requestPath);
            }

            var retryDelay = GetRetryDelay(response, attempt);
            response.Dispose();
            _logger.LogWarning(
                "Transient GitHub request failure for {RequestPath}. Attempt {Attempt} of {MaxAttempts}.",
                requestPath,
                attempt + 1,
                options.MaxRetryAttempts + 1);

            await Task.Delay(retryDelay, cancellationToken);
        }

        throw new InvalidOperationException("GitHub retry loop exited unexpectedly.");
    }

    private HttpRequestMessage CreateRequest(string requestPath, GitHubOptions options)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, CreateRequestUri(requestPath, options.ApiBaseUrl));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("CloudAwesome.PipeLine", "1.0"));
        request.Headers.Add("X-GitHub-Api-Version", options.ApiVersion);

        if (!string.IsNullOrWhiteSpace(options.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Token);
        }

        return request;
    }

    private static Uri CreateRequestUri(string requestPath, string apiBaseUrl)
    {
        if (Uri.TryCreate(requestPath, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        return new Uri(new Uri(apiBaseUrl, UriKind.Absolute), requestPath);
    }

    private static async Task<TResponse> ReadResponseAsync<TResponse>(
        HttpResponseMessage response,
        string requestPath,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<TResponse>(stream, JsonOptions, cancellationToken);

        return payload ?? throw new GitHubRequestFailedException(response.StatusCode, requestPath);
    }

    private static string BuildInitialPagePath(string path, int pageSize)
    {
        var separator = path.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{path}{separator}per_page={pageSize.ToString(CultureInfo.InvariantCulture)}&page=1";
    }

    private static string? GetNextPagePath(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var values))
        {
            return null;
        }

        foreach (var link in string.Join(",", values).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var sections = link.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (sections.Length < 2 || !sections.Any(section => section.Equals("rel=\"next\"", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var uriSection = sections[0];
            if (uriSection.Length > 2 && uriSection[0] == '<' && uriSection[^1] == '>')
            {
                return uriSection[1..^1];
            }
        }

        return null;
    }

    private static bool IsRateLimitResponse(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return true;
        }

        return response.StatusCode == HttpStatusCode.Forbidden
            && response.Headers.TryGetValues("X-RateLimit-Remaining", out var values)
            && values.Any(value => value == "0");
    }

    private static DateTimeOffset? GetRateLimitReset(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("X-RateLimit-Reset", out var values))
        {
            return null;
        }

        var resetValue = values.FirstOrDefault();
        return long.TryParse(resetValue, CultureInfo.InvariantCulture, out var unixSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds)
            : null;
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is { } retryAfter)
        {
            return retryAfter;
        }

        return TimeSpan.FromMilliseconds(50 * (attempt + 1));
    }

    private static string Encode(string value)
    {
        return Uri.EscapeDataString(value);
    }

    private sealed record GitHubWorkflowsResponse
    {
        [JsonPropertyName("workflows")]
        public IReadOnlyList<GitHubWorkflow> Workflows { get; init; } = [];
    }

    private sealed record GitHubWorkflowRunsResponse
    {
        [JsonPropertyName("workflow_runs")]
        public IReadOnlyList<GitHubWorkflowRun> WorkflowRuns { get; init; } = [];
    }
}
