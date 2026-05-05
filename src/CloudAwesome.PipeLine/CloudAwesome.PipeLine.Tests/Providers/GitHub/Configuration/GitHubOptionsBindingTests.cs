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
                ["GitHub:Repositories:0:Owner"] = "cloud-awesome",
                ["GitHub:Repositories:0:Name"] = "pipeline-execution-dashboard",
                ["GitHub:Repositories:0:DisplayName"] = "Pipeline Execution Dashboard",
                ["GitHub:Repositories:0:Pipelines:0:Name"] = "Build",
                ["GitHub:Repositories:0:Pipelines:0:Category"] = "build"
            })
            .Build();

        var options = new GitHubOptions();
        configuration.GetSection(GitHubOptions.SectionName).Bind(options);

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
