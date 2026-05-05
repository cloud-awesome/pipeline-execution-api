using CloudAwesome.PipeLine.Providers.GitHub.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CloudAwesome.PipeLine.Tests.Providers.GitHub.Configuration;

[TestFixture]
public sealed class GitHubOptionsValidatorTests
{
    private readonly GitHubOptionsValidator _validator = new();

    [Test]
    public void ValidConfigurationPasses()
    {
        var result = _validator.Validate(Options.DefaultName, new GitHubOptions());

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void CustomPipelineCategoryPasses()
    {
        var options = CreateValidRepositoryOptions();
        options.Repositories[0].Pipelines[0].Category = "security-scan";

        var result = new GitHubRepositoriesOptionsValidator().Validate(Options.DefaultName, options);

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void MissingRepositoriesFailClearly()
    {
        var result = new GitHubRepositoriesOptionsValidator().Validate(Options.DefaultName, new GitHubRepositoriesOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Failures, Does.Contain("GitHubRepositories:Repositories must contain at least one repository."));
        });
    }

    [Test]
    public void MissingRepositoryFieldsFailClearly()
    {
        var options = new GitHubRepositoriesOptions
        {
            Repositories =
            [
                new GitHubRepositoryOptions()
            ]
        };

        var result = new GitHubRepositoriesOptionsValidator().Validate(Options.DefaultName, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Failures, Does.Contain("GitHubRepositories:Repositories:0:Owner is required."));
            Assert.That(result.Failures, Does.Contain("GitHubRepositories:Repositories:0:Name is required."));
        });
    }

    [Test]
    public void InvalidClientSettingsFailClearly()
    {
        var options = new GitHubOptions();
        options.ApiBaseUrl = "not-a-uri";
        options.ApiVersion = " ";
        options.PageSize = 101;
        options.MaxRetryAttempts = 6;

        var result = _validator.Validate(Options.DefaultName, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(GetFailures(result), Does.Contain("GitHub:ApiBaseUrl must be an absolute URI."));
            Assert.That(GetFailures(result), Does.Contain("GitHub:ApiVersion is required."));
            Assert.That(GetFailures(result), Does.Contain("GitHub:PageSize must be between 1 and 100."));
            Assert.That(GetFailures(result), Does.Contain("GitHub:MaxRetryAttempts must be between 0 and 5."));
        });
    }

    [Test]
    public void MissingPipelineFieldsFailClearly()
    {
        var options = CreateValidRepositoryOptions();
        options.Repositories[0].Pipelines[0] = new GitHubPipelineOptions();

        var result = new GitHubRepositoriesOptionsValidator().Validate(Options.DefaultName, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Failures, Does.Contain("GitHubRepositories:Repositories:0:Pipelines:0:Name is required."));
            Assert.That(result.Failures, Does.Contain("GitHubRepositories:Repositories:0:Pipelines:0:Category is required."));
        });
    }

    [Test]
    public void DuplicateRepositoriesFailClearly()
    {
        var options = CreateValidRepositoryOptions();
        options.Repositories.Add(new GitHubRepositoryOptions
        {
            Owner = "Cloud-Awesome",
            Name = "Pipeline-Execution-Dashboard"
        });

        var result = new GitHubRepositoriesOptionsValidator().Validate(Options.DefaultName, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(GetFailures(result).Single(), Is.EqualTo("GitHubRepositories:Repositories:1 duplicates repository 'Cloud-Awesome/Pipeline-Execution-Dashboard'."));
        });
    }

    [Test]
    public void DuplicatePipelinesFailClearly()
    {
        var options = CreateValidRepositoryOptions();
        options.Repositories[0].Pipelines.Add(new GitHubPipelineOptions
        {
            Name = "build",
            Category = "release"
        });

        var result = new GitHubRepositoriesOptionsValidator().Validate(Options.DefaultName, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(GetFailures(result).Single(), Is.EqualTo("GitHubRepositories:Repositories:0:Pipelines:1 duplicates pipeline 'build'."));
        });
    }

    private static IEnumerable<string> GetFailures(ValidateOptionsResult result)
    {
        return result.Failures ?? [];
    }

    private static GitHubRepositoriesOptions CreateValidRepositoryOptions()
    {
        return new GitHubRepositoriesOptions
        {
            Repositories =
            [
                new GitHubRepositoryOptions
                {
                    Owner = "cloud-awesome",
                    Name = "pipeline-execution-dashboard",
                    DisplayName = "Pipeline Execution Dashboard",
                    Pipelines =
                    [
                        new GitHubPipelineOptions
                        {
                            Name = "Build",
                            Category = "build"
                        }
                    ]
                }
            ]
        };
    }
}
