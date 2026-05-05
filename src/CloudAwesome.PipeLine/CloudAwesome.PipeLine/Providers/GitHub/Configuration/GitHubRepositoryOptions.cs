namespace CloudAwesome.PipeLine.Providers.GitHub.Configuration;

public sealed class GitHubRepositoryOptions
{
    public string? Owner { get; set; }

    public string? Name { get; set; }

    public string? DisplayName { get; set; }

    public List<GitHubPipelineOptions> Pipelines { get; set; } = [];
}
