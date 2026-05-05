using CloudAwesome.PipeLine.Providers.GitHub.Configuration;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace CloudAwesome.PipeLine.Tests.Providers.GitHub.Configuration;

[TestFixture]
public sealed class GitHubOptionsBindingTests
{
    [Test]
    public void OptionsBindFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GitHub:ApiBaseUrl"] = "https://api.github.example/",
                ["GitHub:ApiVersion"] = "2026-03-10",
                ["GitHub:PageSize"] = "50",
                ["GitHub:MaxRetryAttempts"] = "2",
                ["GitHub:Token"] = "test-token"
            })
            .Build();

        var options = new GitHubOptions();
        configuration.GetSection(GitHubOptions.SectionName).Bind(options);

        Assert.Multiple(() =>
        {
            Assert.That(options.ApiBaseUrl, Is.EqualTo("https://api.github.example/"));
            Assert.That(options.ApiVersion, Is.EqualTo("2026-03-10"));
            Assert.That(options.PageSize, Is.EqualTo(50));
            Assert.That(options.MaxRetryAttempts, Is.EqualTo(2));
            Assert.That(options.Token, Is.EqualTo("test-token"));
        });
    }

    [Test]
    public void RepositoryOptionsBindFromBundledJsonShape()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GitHubRepositories:Repositories:0:Owner"] = "cloud-awesome",
                ["GitHubRepositories:Repositories:0:Name"] = "pipeline-execution-dashboard",
                ["GitHubRepositories:Repositories:0:DisplayName"] = "Pipeline Execution Dashboard",
                ["GitHubRepositories:Repositories:0:Pipelines:0:Name"] = "Build",
                ["GitHubRepositories:Repositories:0:Pipelines:0:Category"] = "build"
            })
            .Build();

        var options = new GitHubRepositoriesOptions();
        configuration.GetSection(GitHubRepositoriesOptions.SectionName).Bind(options);

        Assert.Multiple(() =>
        {
            Assert.That(options.Repositories, Has.Count.EqualTo(1));
            Assert.That(options.Repositories[0].Owner, Is.EqualTo("cloud-awesome"));
            Assert.That(options.Repositories[0].Name, Is.EqualTo("pipeline-execution-dashboard"));
            Assert.That(options.Repositories[0].DisplayName, Is.EqualTo("Pipeline Execution Dashboard"));
            Assert.That(options.Repositories[0].Pipelines, Has.Count.EqualTo(1));
            Assert.That(options.Repositories[0].Pipelines[0].Name, Is.EqualTo("Build"));
            Assert.That(options.Repositories[0].Pipelines[0].Category, Is.EqualTo("build"));
        });
    }
}
