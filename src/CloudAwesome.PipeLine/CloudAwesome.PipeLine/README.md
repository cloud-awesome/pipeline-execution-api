# CloudAwesome.PipeLine

Azure Functions reference API for serving pipeline execution dashboard data.

The API is provider-specific at the HTTP boundary. GitHub Actions support starts at `api/github`; future providers such as Azure DevOps should be added beside it, for example `api/azure`, without changing the dashboard response contract.

## Endpoints

| Route | Description |
| --- | --- |
| `GET /api/github` | Returns GitHub Actions data transformed into the dashboard `DashboardData` contract. |

## Requirements

- .NET 8 SDK
- Azure Functions Core Tools v4
- Azurite or another local storage emulator when using `UseDevelopmentStorage=true`

The project uses the Azure Functions isolated worker model on Functions runtime v4.

## Local Setup

1. Copy `local.settings.template.json` to `local.settings.json`.
2. Adjust `github.repositories.json` to include the public repositories and pipelines to query.
3. Keep `local.settings.json` out of source control.
4. Run the Function app from Rider/JetBrains using the configured local host command:

```powershell
host start --pause-on-error --port 7108
```

The default Rider launch profile runs the app on port `7108`.

## Configuration Shape

The MVP configuration is GitHub-specific and should remain isolated from the dashboard contract. Operational settings are loaded from application settings so they can vary by environment. Repository and pipeline mappings are loaded from the bundled `github.repositories.json` file.

`github.repositories.json` must contain at least one repository under `GitHubRepositories:Repositories`. Each repository requires `owner` and `name`. Pipeline mappings are optional, but when provided each mapping requires `name` and `category`. Categories are not restricted to the default dashboard categories, so consumers can introduce custom category names later without changing the API.

Environment-specific application settings use keys such as:

```text
GitHub__ApiBaseUrl
GitHub__ApiVersion
GitHub__Token
GitHub__PageSize
GitHub__MaxRetryAttempts
```

`GitHub__Token` is optional for public repositories, but can be supplied as an app setting for higher rate limits or later private repository support. Do not commit real tokens to `local.settings.json`.

The repository mapping file is copied to the Function publish output and deployed with the app.

Configured pipeline mappings are matched to GitHub workflow names case-insensitively. Workflows without a configured mapping are included with category `other`. Disabled workflows are excluded.

GitHub statuses are normalized to the dashboard status set:

- completed success -> `success`
- completed failure, timed out, or startup failure -> `failure`
- completed cancelled -> `cancelled`
- completed neutral, skipped, action required, or unknown conclusion -> `neutral`
- queued, requested, waiting, or pending -> `queued`
- in progress or unknown non-completed status -> `running`

## CORS

For local development, configure the `Host:CORS` value in `local.settings.json`. The template includes common local React ports.

For Azure deployments, configure CORS on the Function App itself and include the static site or documentation site origin that will host the React dashboard component.

## Development Commands

From the solution directory:

```powershell
dotnet restore
dotnet build
dotnet test
```
