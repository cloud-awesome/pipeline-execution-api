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
        var result = _validator.Validate(Options.DefaultName, CreateValidOptions());

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void CustomPipelineCategoryPasses()
    {
        var options = CreateValidOptions();
        options.Repositories[0].Pipelines[0].Category = "security-scan";

        var result = _validator.Validate(Options.DefaultName, options);

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void MissingRepositoriesFailClearly()
    {
        var result = _validator.Validate(Options.DefaultName, new GitHubOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Failures, Does.Contain("GitHub:Repositories must contain at least one repository."));
        });
    }

    [Test]
    public void MissingRepositoryFieldsFailClearly()
    {
        var options = new GitHubOptions
        {
            Repositories =
            [
                new GitHubRepositoryOptions()
            ]
        };

        var result = _validator.Validate(Options.DefaultName, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Failures, Does.Contain("GitHub:Repositories:0:Owner is required."));
            Assert.That(result.Failures, Does.Contain("GitHub:Repositories:0:Name is required."));
        });
    }

    [Test]
    public void InvalidClientSettingsFailClearly()
    {
        var options = CreateValidOptions();
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
        var options = CreateValidOptions();
        options.Repositories[0].Pipelines[0] = new GitHubPipelineOptions();

        var result = _validator.Validate(Options.DefaultName, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Failures, Does.Contain("GitHub:Repositories:0:Pipelines:0:Name is required."));
            Assert.That(result.Failures, Does.Contain("GitHub:Repositories:0:Pipelines:0:Category is required."));
        });
    }

    [Test]
    public void DuplicateRepositoriesFailClearly()
    {
        var options = CreateValidOptions();
        options.Repositories.Add(new GitHubRepositoryOptions
        {
            Owner = "Cloud-Awesome",
            Name = "Pipeline-Execution-Dashboard"
        });

        var result = _validator.Validate(Options.DefaultName, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(GetFailures(result).Single(), Is.EqualTo("GitHub:Repositories:1 duplicates repository 'Cloud-Awesome/Pipeline-Execution-Dashboard'."));
        });
    }

    [Test]
    public void DuplicatePipelinesFailClearly()
    {
        var options = CreateValidOptions();
        options.Repositories[0].Pipelines.Add(new GitHubPipelineOptions
        {
            Name = "build",
            Category = "release"
        });

        var result = _validator.Validate(Options.DefaultName, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(GetFailures(result).Single(), Is.EqualTo("GitHub:Repositories:0:Pipelines:1 duplicates pipeline 'build'."));
        });
    }

    private static IEnumerable<string> GetFailures(ValidateOptionsResult result)
    {
        return result.Failures ?? [];
    }

    private static GitHubOptions CreateValidOptions()
    {
        return new GitHubOptions
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
