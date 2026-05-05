namespace CloudAwesome.PipeLine.Providers.GitHub.Configuration;

public sealed class GitHubRepositoriesOptions
{
    public const string DefaultFileName = "github.repositories.json";
    public const string SectionName = "GitHubRepositories";

    public List<GitHubRepositoryOptions> Repositories { get; set; } = [];
}
