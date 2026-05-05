using CloudAwesome.PipeLine;
using CloudAwesome.PipeLine.Contracts;
using CloudAwesome.PipeLine.Providers;
using NUnit.Framework;
using System.Text.Json;

namespace CloudAwesome.PipeLine.Tests;

[TestFixture]
public sealed class ProjectSetupTests
{
    [Test]
    public void GitHubEndpointRouteIsProviderSpecific()
    {
        Assert.That(ApiRoutes.GitHubDashboard, Is.EqualTo("github"));
    }

    [Test]
    public void InitialProviderBoundaryIsGitHub()
    {
        Assert.That(PipelineProvider.GitHub.ToString(), Is.EqualTo("GitHub"));
    }

    [Test]
    public void DashboardContractSerializesWithReactContractPropertyNames()
    {
        var data = new DashboardData
        {
            GeneratedAt = DateTimeOffset.Parse("2026-05-05T12:00:00Z"),
            Repositories =
            [
                new DashboardRepository
                {
                    Id = "cloud-awesome/pipeline-execution-dashboard",
                    Name = "pipeline-execution-dashboard",
                    DisplayName = "Pipeline Execution Dashboard",
                    Url = "https://github.com/cloud-awesome/pipeline-execution-dashboard"
                }
            ],
            Pipelines =
            [
                new DashboardPipeline
                {
                    Id = "cloud-awesome/pipeline-execution-dashboard/build",
                    RepositoryId = "cloud-awesome/pipeline-execution-dashboard",
                    Name = "Build",
                    Category = PipelineCategories.Build,
                    Url = "https://github.com/cloud-awesome/pipeline-execution-dashboard/actions"
                }
            ],
            Executions =
            [
                new DashboardExecution
                {
                    Id = "123",
                    RepositoryId = "cloud-awesome/pipeline-execution-dashboard",
                    PipelineId = "cloud-awesome/pipeline-execution-dashboard/build",
                    Status = ExecutionStatuses.Success,
                    StartedAt = DateTimeOffset.Parse("2026-05-05T11:58:00Z"),
                    CompletedAt = DateTimeOffset.Parse("2026-05-05T12:00:00Z"),
                    DurationMs = 120000,
                    Branch = "main",
                    CommitSha = "abc123",
                    Url = "https://github.com/cloud-awesome/pipeline-execution-dashboard/actions/runs/123"
                }
            ]
        };

        var json = JsonSerializer.Serialize(data);

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"generatedAt\""));
            Assert.That(json, Does.Contain("\"repositoryId\""));
            Assert.That(json, Does.Contain("\"pipelineId\""));
            Assert.That(json, Does.Contain("\"commitSha\""));
            Assert.That(json.Contains("workflow", StringComparison.OrdinalIgnoreCase), Is.False);
        });
    }
}
