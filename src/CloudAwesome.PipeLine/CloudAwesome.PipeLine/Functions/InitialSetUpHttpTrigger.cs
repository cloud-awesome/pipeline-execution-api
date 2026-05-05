using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CloudAwesome.PipeLine.Functions;

public class InitialSetUpHttpTrigger
{
	private readonly ILogger<InitialSetUpHttpTrigger> _logger;

	public InitialSetUpHttpTrigger(ILogger<InitialSetUpHttpTrigger> logger)
	{
		_logger = logger;
	}

	[Function("InitialSetUpHttpTrigger")]
	public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
	{
		_logger.LogInformation("C# HTTP trigger function processed a request.");
		return new OkObjectResult("Welcome to Azure Functions!");
	}

}