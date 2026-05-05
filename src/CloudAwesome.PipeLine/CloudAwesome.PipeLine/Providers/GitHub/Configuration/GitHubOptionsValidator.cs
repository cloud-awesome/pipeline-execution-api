using Microsoft.Extensions.Options;

namespace CloudAwesome.PipeLine.Providers.GitHub.Configuration;

public sealed class GitHubOptionsValidator : IValidateOptions<GitHubOptions>
{
    public ValidateOptionsResult Validate(string? name, GitHubOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.Repositories.Count == 0)
        {
            failures.Add("GitHub:Repositories must contain at least one repository.");
            return ValidateOptionsResult.Fail(failures);
        }

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

        ValidateRepositories(options.Repositories, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateRepositories(IReadOnlyList<GitHubRepositoryOptions> repositories, ICollection<string> failures)
    {
        var repositoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var repositoryIndex = 0; repositoryIndex < repositories.Count; repositoryIndex++)
        {
            var repository = repositories[repositoryIndex];
            var repositoryPath = $"GitHub:Repositories:{repositoryIndex}";

            if (string.IsNullOrWhiteSpace(repository.Owner))
            {
                failures.Add($"{repositoryPath}:Owner is required.");
            }

            if (string.IsNullOrWhiteSpace(repository.Name))
            {
                failures.Add($"{repositoryPath}:Name is required.");
            }

            if (!string.IsNullOrWhiteSpace(repository.Owner) && !string.IsNullOrWhiteSpace(repository.Name))
            {
                var repositoryId = $"{repository.Owner.Trim()}/{repository.Name.Trim()}";
                if (!repositoryIds.Add(repositoryId))
                {
                    failures.Add($"{repositoryPath} duplicates repository '{repositoryId}'.");
                }
            }

            ValidatePipelines(repository.Pipelines, repositoryPath, failures);
        }
    }

    private static void ValidatePipelines(
        IReadOnlyList<GitHubPipelineOptions> pipelines,
        string repositoryPath,
        ICollection<string> failures)
    {
        var pipelineNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var pipelineIndex = 0; pipelineIndex < pipelines.Count; pipelineIndex++)
        {
            var pipeline = pipelines[pipelineIndex];
            var pipelinePath = $"{repositoryPath}:Pipelines:{pipelineIndex}";

            if (string.IsNullOrWhiteSpace(pipeline.Name))
            {
                failures.Add($"{pipelinePath}:Name is required.");
            }
            else if (!pipelineNames.Add(pipeline.Name.Trim()))
            {
                failures.Add($"{pipelinePath} duplicates pipeline '{pipeline.Name.Trim()}'.");
            }

            if (string.IsNullOrWhiteSpace(pipeline.Category))
            {
                failures.Add($"{pipelinePath}:Category is required.");
            }
        }
    }
}
