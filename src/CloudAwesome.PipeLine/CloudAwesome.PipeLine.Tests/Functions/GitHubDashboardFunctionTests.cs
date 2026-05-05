using System.Net;
using CloudAwesome.PipeLine.Contracts;
using CloudAwesome.PipeLine.Functions;
using CloudAwesome.PipeLine.Providers;
using CloudAwesome.PipeLine.Providers.GitHub.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CloudAwesome.PipeLine.Tests.Functions;

[TestFixture]
public sealed class GitHubDashboardFunctionTests
{
    [Test]
    public async Task ReturnsDashboardData()
    {
        var dashboardData = new DashboardData
        {
            GeneratedAt = DateTimeOffset.Parse("2026-05-05T12:30:00Z"),
            Repositories = [],
            Pipelines = [],
            Executions = []
        };
        var function = CreateFunction(new StubProvider(dashboardData));
        var request = CreateRequest();

        var result = await function.Run(request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult?.Value, Is.SameAs(dashboardData));
            Assert.That(request.HttpContext.Response.Headers.CacheControl.ToString(), Is.EqualTo("no-store"));
        });
    }

    [Test]
    public async Task ReturnsSafeConfigurationError()
    {
        var exception = new OptionsValidationException("GitHubRepositories", typeof(object), ["GitHubRepositories:Repositories must contain at least one repository."]);
        var function = CreateFunction(new StubProvider(exception));

        var result = await function.Run(CreateRequest(), CancellationToken.None);

        AssertError(result, StatusCodes.Status500InternalServerError, "configuration_invalid", "GitHub dashboard configuration is invalid.");

        var objectResult = result as ObjectResult;
        var error = objectResult?.Value as ErrorResponse;
        Assert.That(error?.Details, Does.Contain("GitHubRepositories:Repositories must contain at least one repository."));
    }

    [Test]
    public async Task ReturnsSafeRateLimitError()
    {
        var resetAt = DateTimeOffset.Parse("2026-05-05T13:00:00Z");
        var exception = new GitHubRateLimitExceededException(HttpStatusCode.Forbidden, "private/request/path", resetAt);
        var function = CreateFunction(new StubProvider(exception));

        var result = await function.Run(CreateRequest(), CancellationToken.None);

        var objectResult = AssertError(result, StatusCodes.Status503ServiceUnavailable, "github_rate_limited", "GitHub rate limit was exceeded.");
        var error = objectResult.Value as ErrorResponse;
        Assert.That(error?.RetryAfter, Is.EqualTo(resetAt));
    }

    [Test]
    public async Task ReturnsSafeGitHubFailureError()
    {
        var exception = new GitHubRequestFailedException(HttpStatusCode.NotFound, "private/request/path");
        var function = CreateFunction(new StubProvider(exception));

        var result = await function.Run(CreateRequest(), CancellationToken.None);

        AssertError(result, StatusCodes.Status502BadGateway, "github_request_failed", "GitHub data could not be retrieved.");
    }

    private static GitHubDashboardFunction CreateFunction(StubProvider provider)
    {
        return new GitHubDashboardFunction(provider, NullLogger<GitHubDashboardFunction>.Instance);
    }

    private static HttpRequest CreateRequest()
    {
        return new DefaultHttpContext().Request;
    }

    private static ObjectResult AssertError(IActionResult result, int statusCode, string errorCode, string message)
    {
        var objectResult = result as ObjectResult;
        var error = objectResult?.Value as ErrorResponse;

        Assert.Multiple(() =>
        {
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult?.StatusCode, Is.EqualTo(statusCode));
            Assert.That(error?.Error, Is.EqualTo(errorCode));
            Assert.That(error?.Message, Is.EqualTo(message));
            Assert.That(error?.Message, Does.Not.Contain("private/request/path"));
            Assert.That(error?.Message, Does.Not.Contain("secret-like"));
        });

        return objectResult!;
    }

    private sealed class StubProvider : IPipelineDashboardProvider
    {
        private readonly DashboardData? _dashboardData;
        private readonly Exception? _exception;

        public StubProvider(DashboardData dashboardData)
        {
            _dashboardData = dashboardData;
        }

        public StubProvider(Exception exception)
        {
            _exception = exception;
        }

        public PipelineProvider Provider => PipelineProvider.GitHub;

        public Task<DashboardData> GetDashboardDataAsync(CancellationToken cancellationToken)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_dashboardData!);
        }
    }
}
