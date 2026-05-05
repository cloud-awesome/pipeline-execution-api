using Microsoft.Extensions.Options;

namespace CloudAwesome.PipeLine.Providers.GitHub.Configuration;

public sealed class GitHubOptionsValidator : IValidateOptions<GitHubOptions>
{
    public ValidateOptionsResult Validate(string? name, GitHubOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (!Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out var apiBaseUri))
        {
            failures.Add("GitHub:ApiBaseUrl must be an absolute URI.");
        }
        else if (apiBaseUri.Scheme is not "https" and not "http")
        {
            failures.Add("GitHub:ApiBaseUrl must use http or https.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiVersion))
        {
            failures.Add("GitHub:ApiVersion is required.");
        }

        if (options.PageSize is < 1 or > 100)
        {
            failures.Add("GitHub:PageSize must be between 1 and 100.");
        }

        if (options.MaxRetryAttempts is < 0 or > 5)
        {
            failures.Add("GitHub:MaxRetryAttempts must be between 0 and 5.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
