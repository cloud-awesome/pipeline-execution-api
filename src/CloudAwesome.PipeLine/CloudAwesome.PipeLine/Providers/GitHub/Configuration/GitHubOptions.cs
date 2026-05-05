namespace CloudAwesome.PipeLine.Providers.GitHub.Configuration;

public sealed class GitHubOptions
{
    public const string DefaultApiBaseUrl = "https://api.github.com/";
    public const string DefaultApiVersion = "2026-03-10";
    public const string SectionName = "GitHub";

    public string ApiBaseUrl { get; set; } = DefaultApiBaseUrl;

    public string ApiVersion { get; set; } = DefaultApiVersion;

    public string? Token { get; set; }

    public int PageSize { get; set; } = 100;

    public int MaxRetryAttempts { get; set; } = 3;
}
