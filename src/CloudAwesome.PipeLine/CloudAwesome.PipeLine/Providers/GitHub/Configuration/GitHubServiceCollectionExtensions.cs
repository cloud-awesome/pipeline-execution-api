using CloudAwesome.PipeLine.Providers;
using CloudAwesome.PipeLine.Providers.GitHub.Client;
using CloudAwesome.PipeLine.Providers.GitHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudAwesome.PipeLine.Providers.GitHub.Configuration;

public static class GitHubServiceCollectionExtensions
{
    public static IServiceCollection AddGitHubConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<GitHubOptions>()
            .Bind(configuration.GetSection(GitHubOptions.SectionName));

        services.AddSingleton<IValidateOptions<GitHubOptions>, GitHubOptionsValidator>();
        services.AddSingleton<IGitHubClient>(serviceProvider =>
            new GitHubClient(
                new HttpClient(),
                serviceProvider.GetRequiredService<IOptionsMonitor<GitHubOptions>>(),
                serviceProvider.GetRequiredService<ILogger<GitHubClient>>()));
        services.AddSingleton<IPipelineDashboardProvider, GitHubDashboardProvider>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
