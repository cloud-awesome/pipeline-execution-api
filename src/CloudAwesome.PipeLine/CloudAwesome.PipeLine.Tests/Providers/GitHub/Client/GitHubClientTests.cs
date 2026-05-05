using System.Net;
using CloudAwesome.PipeLine.Providers.GitHub.Client;
using CloudAwesome.PipeLine.Providers.GitHub.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CloudAwesome.PipeLine.Tests.Providers.GitHub.Client;

[TestFixture]
public sealed class GitHubClientTests
{
    [Test]
    public async Task GetsRepositoryWorkflowsWithGitHubHeaders()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueJson("""
            {
              "workflows": [
                {
                  "id": 101,
                  "name": "Build",
                  "path": ".github/workflows/build.yml",
                  "state": "active",
                  "html_url": "https://github.com/cloud-awesome/repo/actions/workflows/build.yml"
                }
              ]
            }
            """);

        var client = CreateClient(handler, new GitHubOptions { Token = "test-token" });

        var workflows = await client.GetRepositoryWorkflowsAsync("cloud-awesome", "repo", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(workflows, Has.Count.EqualTo(1));
            Assert.That(workflows[0].Id, Is.EqualTo(101));
            Assert.That(workflows[0].Name, Is.EqualTo("Build"));
            Assert.That(handler.Requests[0].RequestUri?.ToString(), Is.EqualTo("https://api.github.com/repos/cloud-awesome/repo/actions/workflows?per_page=100&page=1"));
            Assert.That(handler.Requests[0].Headers.Accept.Single().MediaType, Is.EqualTo("application/vnd.github+json"));
            Assert.That(handler.Requests[0].Headers.UserAgent.ToString(), Is.EqualTo("CloudAwesome.PipeLine/1.0"));
            Assert.That(handler.Requests[0].Headers.GetValues("X-GitHub-Api-Version").Single(), Is.EqualTo(GitHubOptions.DefaultApiVersion));
            Assert.That(handler.Requests[0].Headers.Authorization?.Scheme, Is.EqualTo("Bearer"));
            Assert.That(handler.Requests[0].Headers.Authorization?.Parameter, Is.EqualTo("test-token"));
        });
    }

    [Test]
    public async Task GetsWorkflowRunsForWorkflow()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueJson("""
            {
              "workflow_runs": [
                {
                  "id": 501,
                  "name": "Build",
                  "workflow_id": 101,
                  "status": "completed",
                  "conclusion": "success",
                  "head_branch": "main",
                  "head_sha": "abc123",
                  "run_started_at": "2026-05-05T11:58:00Z",
                  "created_at": "2026-05-05T11:57:00Z",
                  "updated_at": "2026-05-05T12:00:00Z",
                  "html_url": "https://github.com/cloud-awesome/repo/actions/runs/501"
                }
              ]
            }
            """);

        var client = CreateClient(handler);

        var runs = await client.GetWorkflowRunsAsync("cloud-awesome", "repo", 101, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(runs, Has.Count.EqualTo(1));
            Assert.That(runs[0].WorkflowId, Is.EqualTo(101));
            Assert.That(runs[0].Conclusion, Is.EqualTo("success"));
            Assert.That(handler.Requests[0].RequestUri?.ToString(), Is.EqualTo("https://api.github.com/repos/cloud-awesome/repo/actions/workflows/101/runs?per_page=100&page=1"));
        });
    }

    [Test]
    public async Task FollowsPaginationLinks()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueJson(
            """
            {
              "workflows": [
                { "id": 101, "name": "Build" }
              ]
            }
            """,
            links: ["<https://api.github.com/repositories/1/actions/workflows?per_page=1&page=2>; rel=\"next\""]);
        handler.EnqueueJson("""
            {
              "workflows": [
                { "id": 102, "name": "Publish" }
              ]
            }
            """);

        var client = CreateClient(handler, new GitHubOptions { PageSize = 1 });

        var workflows = await client.GetRepositoryWorkflowsAsync("cloud-awesome", "repo", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(workflows.Select(workflow => workflow.Name), Is.EqualTo(new[] { "Build", "Publish" }));
            Assert.That(handler.Requests, Has.Count.EqualTo(2));
            Assert.That(handler.Requests[1].RequestUri?.ToString(), Is.EqualTo("https://api.github.com/repositories/1/actions/workflows?per_page=1&page=2"));
        });
    }

    [Test]
    public void RateLimitResponseThrowsRateLimitException()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Headers =
            {
                { "X-RateLimit-Remaining", "0" },
                { "X-RateLimit-Reset", "1777982400" }
            }
        });

        var client = CreateClient(handler);

        var exception = Assert.ThrowsAsync<GitHubRateLimitExceededException>(
            () => client.GetRepositoryWorkflowsAsync("cloud-awesome", "repo", CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception?.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(exception?.ResetAt, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(1777982400)));
        });
    }

    [Test]
    public async Task TransientFailureIsRetried()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.BadGateway));
        handler.EnqueueJson("""
            {
              "workflows": [
                { "id": 101, "name": "Build" }
              ]
            }
            """);

        var client = CreateClient(handler, new GitHubOptions { MaxRetryAttempts = 1 });

        var workflows = await client.GetRepositoryWorkflowsAsync("cloud-awesome", "repo", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(workflows, Has.Count.EqualTo(1));
            Assert.That(handler.Requests, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void NonTransientFailureThrowsRequestException()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.NotFound));

        var client = CreateClient(handler);

        var exception = Assert.ThrowsAsync<GitHubRequestFailedException>(
            () => client.GetRepositoryWorkflowsAsync("cloud-awesome", "missing", CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception?.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(exception?.Message, Does.Not.Contain("test-token"));
        });
    }

    private static GitHubClient CreateClient(StubHttpMessageHandler handler, GitHubOptions? options = null)
    {
        return new GitHubClient(
            new HttpClient(handler),
            new OptionsMonitorStub<GitHubOptions>(options ?? new GitHubOptions()),
            NullLogger<GitHubClient>.Instance);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();

        public List<HttpRequestMessage> Requests { get; } = [];

        public void Enqueue(HttpResponseMessage response)
        {
            _responses.Enqueue(response);
        }

        public void EnqueueJson(string json, IReadOnlyList<string>? links = null)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };

            if (links is not null)
            {
                response.Headers.Add("Link", links);
            }

            Enqueue(response);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No stub HTTP response was queued.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed class OptionsMonitorStub<TOptions>(TOptions currentValue) : IOptionsMonitor<TOptions>
    {
        public TOptions CurrentValue { get; } = currentValue;

        public TOptions Get(string? name)
        {
            return CurrentValue;
        }

        public IDisposable? OnChange(Action<TOptions, string?> listener)
        {
            return null;
        }
    }
}
