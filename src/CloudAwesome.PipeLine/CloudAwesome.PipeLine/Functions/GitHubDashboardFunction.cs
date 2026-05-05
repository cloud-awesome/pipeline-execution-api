using CloudAwesome.PipeLine.Contracts;
using CloudAwesome.PipeLine.Providers;
using CloudAwesome.PipeLine.Providers.GitHub.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudAwesome.PipeLine.Functions;

public sealed class GitHubDashboardFunction
{
    private readonly IPipelineDashboardProvider _provider;
    private readonly ILogger<GitHubDashboardFunction> _logger;

    public GitHubDashboardFunction(
        IPipelineDashboardProvider provider,
        ILogger<GitHubDashboardFunction> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    [Function(nameof(GitHubDashboardFunction))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.GitHubDashboard)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        request.HttpContext.Response.Headers["Cache-Control"] = "no-store";

        try
        {
            _logger.LogInformation("Generating GitHub dashboard data.");
            var dashboardData = await _provider.GetDashboardDataAsync(cancellationToken);
            return new OkObjectResult(dashboardData);
        }
        catch (OptionsValidationException exception)
        {
            _logger.LogError(exception, "GitHub dashboard configuration is invalid.");
            return new ObjectResult(new ErrorResponse
            {
                Error = "configuration_invalid",
                Message = "GitHub dashboard configuration is invalid."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        catch (GitHubRateLimitExceededException exception)
        {
            _logger.LogWarning(exception, "GitHub rate limit was exceeded.");
            return new ObjectResult(new ErrorResponse
            {
                Error = "github_rate_limited",
                Message = "GitHub rate limit was exceeded.",
                RetryAfter = exception.ResetAt
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }
        catch (GitHubRequestFailedException exception)
        {
            _logger.LogWarning(exception, "GitHub request failed.");
            return new ObjectResult(new ErrorResponse
            {
                Error = "github_request_failed",
                Message = "GitHub data could not be retrieved."
            })
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }
    }
}
