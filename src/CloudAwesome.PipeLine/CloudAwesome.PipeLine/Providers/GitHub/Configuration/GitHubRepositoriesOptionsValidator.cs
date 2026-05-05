using Microsoft.Extensions.Options;

namespace CloudAwesome.PipeLine.Providers.GitHub.Configuration;

public sealed class GitHubRepositoriesOptionsValidator : IValidateOptions<GitHubRepositoriesOptions>
{
    public ValidateOptionsResult Validate(string? name, GitHubRepositoriesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.Repositories.Count == 0)
        {
            failures.Add("GitHubRepositories:Repositories must contain at least one repository.");
            return ValidateOptionsResult.Fail(failures);
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
            var repositoryPath = $"GitHubRepositories:Repositories:{repositoryIndex}";

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
