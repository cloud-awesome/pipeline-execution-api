using CloudAwesome.PipeLine.Contracts;
using CloudAwesome.PipeLine.Providers;
using CloudAwesome.PipeLine.Providers.GitHub;
using CloudAwesome.PipeLine.Providers.GitHub.Client;
using CloudAwesome.PipeLine.Providers.GitHub.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace CloudAwesome.PipeLine.Tests.Providers.GitHub;

[TestFixture]
public sealed class GitHubDashboardProviderTests
{
    [Test]
    public async Task MapsConfiguredGitHubDataToDashboardData()
    {
        var client = new FakeGitHubClient
        {
            Workflows =
            [
                new GitHubWorkflow
                {
                    Id = 101,
                    Name = "Build",
                    State = "active",
                    HtmlUrl = "https://github.com/cloud-awesome/repo/actions/workflows/build.yml"
                },
                new GitHubWorkflow
                {
                    Id = 102,
                    Name = "Retired",
                    State = "disabled_manually"
                },
                new GitHubWorkflow
                {
                    Id = 103,
                    Name = "Security Scan",
                    State = "active"
                }
            ],
            RunsByWorkflowId =
            {
                [101] =
                [
                    new GitHubWorkflowRun
                    {
                        Id = 501,
                        WorkflowId = 101,
                        Status = "completed",
                        Conclusion = "success",
                        RunStartedAt = DateTimeOffset.Parse("2026-05-05T11:58:00Z"),
                        CreatedAt = DateTimeOffset.Parse("2026-05-05T11:57:00Z"),
                        UpdatedAt = DateTimeOffset.Parse("2026-05-05T12:00:00Z"),
                        HeadBranch = "main",
                        HeadSha = "abc123",
                        HtmlUrl = "https://github.com/cloud-awesome/repo/actions/runs/501"
                    },
                    new GitHubWorkflowRun
                    {
                        Id = 502,
                        WorkflowId = 101,
                        Status = "in_progress",
                        CreatedAt = DateTimeOffset.Parse("2026-05-05T12:05:00Z"),
                        UpdatedAt = DateTimeOffset.Parse("2026-05-05T12:06:00Z")
                    }
                ],
                [103] =
                [
                    new GitHubWorkflowRun
                    {
                        Id = 503,
                        WorkflowId = 103,
                        Status = "queued",
                        CreatedAt = DateTimeOffset.Parse("2026-05-05T12:07:00Z"),
                        UpdatedAt = DateTimeOffset.Parse("2026-05-05T12:07:00Z")
                    }
                ]
            }
        };
        var provider = CreateProvider(client);

        var data = await provider.GetDashboardDataAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(provider.Provider, Is.EqualTo(PipelineProvider.GitHub));
            Assert.That(data.GeneratedAt, Is.EqualTo(DateTimeOffset.Parse("2026-05-05T12:30:00Z")));
            Assert.That(data.Repositories, Has.Count.EqualTo(1));
            Assert.That(data.Repositories[0].Id, Is.EqualTo("cloud-awesome/repo"));
            Assert.That(data.Repositories[0].Url, Is.EqualTo("https://github.com/cloud-awesome/repo"));
            Assert.That(data.Pipelines, Has.Count.EqualTo(2));
            Assert.That(data.Pipelines[0].Name, Is.EqualTo("Build"));
            Assert.That(data.Pipelines[0].Category, Is.EqualTo(PipelineCategories.Build));
            Assert.That(data.Pipelines[1].Name, Is.EqualTo("Security Scan"));
            Assert.That(data.Pipelines[1].Category, Is.EqualTo(PipelineCategories.Other));
            Assert.That(data.Executions, Has.Count.EqualTo(3));
            Assert.That(data.Executions[0].Status, Is.EqualTo(ExecutionStatuses.Success));
            Assert.That(data.Executions[0].StartedAt, Is.EqualTo(DateTimeOffset.Parse("2026-05-05T11:58:00Z")));
            Assert.That(data.Executions[0].CompletedAt, Is.EqualTo(DateTimeOffset.Parse("2026-05-05T12:00:00Z")));
            Assert.That(data.Executions[0].DurationMs, Is.EqualTo(120000));
            Assert.That(data.Executions[1].Status, Is.EqualTo(ExecutionStatuses.Running));
            Assert.That(data.Executions[1].CompletedAt, Is.Null);
            Assert.That(data.Executions[2].Status, Is.EqualTo(ExecutionStatuses.Queued));
        });
    }

    [TestCase("completed", "failure", ExecutionStatuses.Failure)]
    [TestCase("completed", "timed_out", ExecutionStatuses.Failure)]
    [TestCase("completed", "cancelled", ExecutionStatuses.Cancelled)]
    [TestCase("completed", "skipped", ExecutionStatuses.Neutral)]
    [TestCase("completed", "unexpected", ExecutionStatuses.Neutral)]
    [TestCase("requested", null, ExecutionStatuses.Queued)]
    [TestCase("waiting", null, ExecutionStatuses.Queued)]
    [TestCase("unknown", null, ExecutionStatuses.Running)]
    public async Task NormalizesGitHubStatuses(string status, string? conclusion, string expectedStatus)
    {
        var client = new FakeGitHubClient
        {
            Workflows =
            [
                new GitHubWorkflow
                {
                    Id = 101,
                    Name = "Build",
                    State = "active"
                }
            ],
            RunsByWorkflowId =
            {
                [101] =
                [
                    new GitHubWorkflowRun
                    {
                        Id = 501,
                        WorkflowId = 101,
                        Status = status,
                        Conclusion = conclusion,
                        CreatedAt = DateTimeOffset.Parse("2026-05-05T12:00:00Z"),
                        UpdatedAt = DateTimeOffset.Parse("2026-05-05T12:01:00Z")
                    }
                ]
            }
        };
        var provider = CreateProvider(client);

        var data = await provider.GetDashboardDataAsync(CancellationToken.None);

        Assert.That(data.Executions.Single().Status, Is.EqualTo(expectedStatus));
    }

    private static GitHubDashboardProvider CreateProvider(FakeGitHubClient client)
    {
        return new GitHubDashboardProvider(
            client,
            new TestOptionsMonitor<GitHubRepositoriesOptions>(CreateRepositoryOptions()),
            new ManualTimeProvider(DateTimeOffset.Parse("2026-05-05T12:30:00Z")),
            NullLogger<GitHubDashboardProvider>.Instance);
    }

    private static GitHubRepositoriesOptions CreateRepositoryOptions()
    {
        return new GitHubRepositoriesOptions
        {
            Repositories =
            [
                new GitHubRepositoryOptions
                {
                    Owner = "cloud-awesome",
                    Name = "repo",
                    DisplayName = "Repo",
                    Pipelines =
                    [
                        new GitHubPipelineOptions
                        {
                            Name = "Build",
                            Category = PipelineCategories.Build
                        }
                    ]
                }
            ]
        };
    }

    private sealed class FakeGitHubClient : IGitHubClient
    {
        public IReadOnlyList<GitHubWorkflow> Workflows { get; init; } = [];

        public Dictionary<long, IReadOnlyList<GitHubWorkflowRun>> RunsByWorkflowId { get; } = [];

        public Task<IReadOnlyList<GitHubWorkflow>> GetRepositoryWorkflowsAsync(
            string owner,
            string repositoryName,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Workflows);
        }

        public Task<IReadOnlyList<GitHubWorkflowRun>> GetWorkflowRunsAsync(
            string owner,
            string repositoryName,
            long workflowId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(RunsByWorkflowId.GetValueOrDefault(workflowId, []));
        }
    }
}
