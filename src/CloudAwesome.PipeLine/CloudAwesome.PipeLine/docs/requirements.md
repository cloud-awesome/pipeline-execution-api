# Pipeline Execution Dashboard Project Definition

## Project Summary

Build a reusable React-based dashboard component for presenting pipeline execution status across one or more repositories or systems.

The React component must be platform-neutral. It should not know about GitHub, Azure DevOps, GitLab, Jenkins, or any other provider directly. Instead, it renders a supplied `DashboardData` contract.

Data acquisition, authentication, provider-specific transformation, caching, and middleware concerns are outside the React component. The consuming application is responsible for querying and transforming data into the agreed dashboard contract.

A separate Azure Function reference implementation will target GitHub Actions and expose data in the dashboard contract format. This will serve as a practical example for public and private repository scenarios.

## Terminology

Preferred terms:

- Pipeline
- Execution
- Repository
- Category
- Status

Avoid provider-specific terms such as:

- Workflow
- Workflow run
- GitHub Action run

## High-Level Architecture

```text
Provider-specific source
    e.g. GitHub Actions
        │
        ▼
Middleware / API / Azure Function / Static JSON / Other
        │
        ▼
DashboardData contract
        │
        ▼
React dashboard component
````

## React Component Scope

The React package should:

* Render supplied pipeline dashboard data.
* Provide summary visualisations.
* Provide filtering.
* Provide default CSS.
* Make CSS overriding a first-class feature.
* Be fully typed with TypeScript.
* Be fully unit tested.
* Be published as a versioned npm package.
* Include CI/CD for test, build, package, and publish.

The React package should not:

* Call GitHub (or any other provider) directly.
* Handle authentication. (This is the responsibility of the consuming app.)
* Poll provider APIs directly. (This is the responsibility of the consuming app.)
* Know about provider-specific concepts.
* Require Azure Functions.
* Require a specific hosting platform.

## Azure Function Scope

The Azure Function reference project should:

* Target GitHub Actions.
* Query configured repositories.
* Query configured pipeline definitions and executions.
* Transform GitHub-specific data into the `DashboardData` contract.
* Support public repositories for MVP.
* Provide a route suitable for consumption by the React component.
* Be written in C#.
* Include a comprehensive automated test suite.
* Be suitable as a reference implementation for private repository support post-MVP.

As this is a reference project (albeit the first real-world implementation), it should be developed and maintained completely separately to the React dashboard component.

## Proposed DashboardData Contract

```ts
export interface DashboardData {
  generatedAt: string;
  repositories: DashboardRepository[];
  pipelines: DashboardPipeline[];
  executions: DashboardExecution[];
}

export interface DashboardRepository {
  id: string;
  name: string;
  displayName?: string;
  url?: string;
}

export interface DashboardPipeline {
  id: string;
  repositoryId: string;
  name: string;
  category?: PipelineCategory | string;
  url?: string;
}

export interface DashboardExecution {
  id: string;
  repositoryId: string;
  pipelineId: string;
  status: ExecutionStatus | string;
  startedAt: string;
  completedAt?: string;
  durationMs?: number;
  branch?: string;
  commitSha?: string;
  url?: string;
}

export type PipelineCategory =
  | "build"
  | "release"
  | "deploy"
  | "test"
  | "other";

export type ExecutionStatus =
  | "success"
  | "failure"
  | "cancelled"
  | "running"
  | "queued"
  | "neutral";
```

## Proposed React Component API

```tsx
<PipelineDashboard
  data={dashboardData}
  defaultFilters={{
    categories: ["build", "release"],
    statuses: ["success", "failure", "cancelled"]
  }}
/>
```

Additional props for ongoing development:

```ts
interface PipelineDashboardProps {
  data: DashboardData;
  defaultFilters?: DashboardFilters;
  showFilters?: boolean;
  showSummary?: boolean;
  showCharts?: boolean;
  showExecutionTable?: boolean;
  className?: string;
  onRepositoryClick?: (repository: DashboardRepository) => void;
  onPipelineClick?: (pipeline: DashboardPipeline) => void;
  onExecutionClick?: (execution: DashboardExecution) => void;
}
```

# React Dashboard Backlog

## Epic 1: Project Setup

### Story 1.1: Create TypeScript React Package

As a package maintainer, I want a clean TypeScript React package structure so that the dashboard can be built, tested, and published reliably.

Tasks:

* Create package structure.
* Configure TypeScript.
* Configure React build tooling.
* Configure package exports.
* Configure npm package metadata.
* Add README.
* Add licence.
* Add changelog.
* Add contribution guidelines.

Acceptance criteria:

* Package builds successfully.
* Type declarations are emitted.
* Package can be consumed by another React app.
* No provider-specific dependency exists in the React package.
* Tests should be suitably compartmentalised and not mixed in with the package.

### Story 1.2: Configure Development Tooling

Tasks:

* Configure ESLint.
* Configure Prettier.
* Configure type checking.
* Configure test runner.
* Configure coverage reporting.
* Configure local example app.
* Configure package scripts.

Acceptance criteria:

* `npm run lint` passes.
* `npm run typecheck` passes.
* `npm test` passes.
* `npm run build` produces distributable output.

## Epic 2: Data Contract

### Story 2.1: Define Dashboard Types

Tasks:

* Implement TypeScript interfaces.
* Export all public types.
* Add type-level tests where appropriate.
* Add fixture data for tests and examples.

Acceptance criteria:

* Consumers can import dashboard types.
* Fixtures represent realistic multi-repository data.
* Types do not contain GitHub-specific terminology.

### Story 2.2: Add Data Validation Utilities

Tasks:

* Decide whether runtime validation is needed.
* If needed, implement schema validation.
* Provide helpful validation errors.
* Add tests for invalid data.

Acceptance criteria:

* Malformed data can be detected.
* Validation does not prevent consumers from using custom categories or statuses.

## Epic 3: Core Rendering

### Story 3.1: Implement Main PipelineDashboard Component

Tasks:

* Create root dashboard component.
* Accept `DashboardData`.
* Render empty state.
* Render loading-like placeholder only if explicitly supported.
* Apply root CSS class.
* Support optional `className`.

Acceptance criteria:

* Component renders with valid data.
* Component renders a useful empty state.
* Component does not fetch data.

### Story 3.2: Implement Summary Cards

Tasks:

* Calculate execution counts by status.
* Render summary cards.
* Support custom or unknown statuses.
* Add accessible labels.

Acceptance criteria:

* Summary counts are correct.
* Unknown statuses are displayed safely.
* Component is covered by unit tests.

### Story 3.3: Implement Pipeline Status Table

Tasks:

* Group latest execution per pipeline.
* Display repository.
* Display pipeline name.
* Display category.
* Display last status.
* Display started/completed time.
* Display duration.
* Display links where URLs are provided.

Acceptance criteria:

* Latest execution is selected correctly.
* Missing optional fields do not break rendering.
* Links are accessible.
* Table is test covered.

### Story 3.4: Implement Execution History Chart

Tasks:

* Choose chart library.
* Implement stacked bar visualisation.
* Group executions by repository and time bucket.
* Stack by status.
* Support filtered data.
* Handle no-data state.

Acceptance criteria:

* Chart renders from fixture data.
* Grouping logic is unit tested independently.
* Visual component is tested sufficiently without brittle chart internals.

## Epic 4: Filtering

### Story 4.1: Implement Filter State Model

Tasks:

* Define filter interface.
* Support category filters.
* Support repository filters.
* Support status filters.
* Support default filters.
* Support all/none behaviour.

Acceptance criteria:

* Default filters are applied on first render.
* Filter state can represent all required UI cases.
* Filtering logic is unit tested.

### Story 4.2: Implement Filter UI

Tasks:

* Add category filter.
* Add repository filter.
* Add status filter.
* Add reset filters action.
* Add accessible form controls.

Acceptance criteria:

* Users can filter by category.
* Users can filter by repository.
* Users can filter by status.
* Filters update summary, chart, and table.
* Filter UI passes accessibility checks.

### Story 4.3: Add Optional Controlled Filter Support

Tasks:

* Evaluate whether controlled filters are needed for MVP.
* If included, add `filters` and `onFiltersChange` props.
* Add tests.

Acceptance criteria:

* Component can be used in controlled mode if implemented.
* Uncontrolled mode remains simple.

## Epic 5: Styling and Theming

### Story 5.1: Provide Default CSS

Tasks:

* Create 7-1 SCSS architecture structure. 
* Create default stylesheet.
* Define layout classes.
* Define card styles.
* Define table styles.
* Define filter styles.
* Define chart container styles.

Acceptance criteria:

* Dashboard looks usable out of the box.
* Styles are not tied to Docusaurus.
* Styles are easy to override.
* Styles follow the 7-1 SCSS pattern.

### Story 5.2: Add CSS Variables

Tasks:

* Define CSS variables for colours.
* Define CSS variables for spacing.
* Define CSS variables for border radius.
* Define CSS variables for typography.
* Define CSS variables for status colours.

Acceptance criteria:

* Consumers can override theme using CSS variables.
* Default CSS remains optional.
* Documentation explains how to override styles.
* Variables are not tied to specific components.
* Variables are implemented via the 7-1 SCSS pattern.

### Story 5.3: Ensure Host Compatibility

Tasks:

* Test in a plain React app.
* Test in Docusaurus.
* Avoid global resets.
* Avoid invasive selectors.
* Avoid CSS-in-JS lock-in.

Acceptance criteria:

* Dashboard can be embedded in Docusaurus.
* Host site CSS can override defaults.
* Dashboard does not break surrounding layout.

## Epic 6: Accessibility

### Story 6.1: Accessibility Review

Tasks:

* Ensure semantic HTML.
* Ensure table headers are correct.
* Ensure controls have labels.
* Ensure status indicators are not colour-only.
* Ensure keyboard navigation works.
* Add accessibility tests.

Acceptance criteria:

* Core UI is usable with keyboard.
* Important state is available to screen readers.
* Automated accessibility tests pass.

## Epic 7: Testing

### Story 7.1: Unit Test Core Logic

Tasks:

* Test latest execution selection.
* Test status aggregation.
* Test category filtering.
* Test repository filtering.
* Test status filtering.
* Test chart grouping.
* Test duration formatting.
* Test date formatting.

Acceptance criteria:

* Core business logic has high coverage.
* Edge cases are covered.
* Tests are deterministic.

### Story 7.2: Component Test Suite

Tasks:

* Test dashboard rendering.
* Test empty state.
* Test summary cards.
* Test filter interactions.
* Test table rendering.
* Test links.
* Test unknown statuses and categories.
* Test missing optional data.

Acceptance criteria:

* Main user interactions are covered.
* Tests use React Testing Library.
* Tests avoid implementation details where possible.

### Story 7.3: Accessibility Test Suite

Tasks:

* Add jest-axe or equivalent.
* Test main dashboard.
* Test filter UI.
* Test table output.

Acceptance criteria:

* Automated accessibility tests pass.
* Accessibility regressions fail CI.

### Story 7.4: Test Coverage Enforcement

Tasks:

* Configure coverage thresholds.
* Add coverage reporting to CI.
* Decide thresholds for statements, branches, functions, and lines.

Acceptance criteria:

* CI fails below agreed threshold.
* Coverage report is available from pipeline output.

## Epic 8: Documentation and Examples

### Story 8.1: Write Usage Documentation

Tasks:

* Explain package installation.
* Explain `DashboardData`.
* Explain component props.
* Explain CSS import.
* Explain CSS variable overrides.
* Explain non-platform-specific design.

Acceptance criteria:

* A new consumer can install and render the dashboard.
* Documentation includes realistic sample data.

### Story 8.2: Provide Static JSON Example

Tasks:

* Create example JSON file.
* Create example React app page.
* Show fetching data and passing into component.

Acceptance criteria:

* Example runs locally.
* Example demonstrates the intended integration pattern.

### Story 8.3: Provide Docusaurus Example

Tasks:

* Create Docusaurus integration example.
* Document use in a documentation site.
* Show CSS override pattern.

Acceptance criteria:

* Component can be used in a Docusaurus page.
* Example is aligned with intended first consumer usage.

## Epic 9: CI/CD and Publishing

### Story 9.1: Create GitHub Actions CI Pipeline

Tasks:

* Install dependencies.
* Run lint.
* Run typecheck.
* Run unit tests.
* Run accessibility tests.
* Run coverage.
* Build package.

Acceptance criteria:

* Pull requests run full validation.
* Main branch runs full validation.
* CI fails on test or build failure.

### Story 9.2: Add Versioning Strategy

Tasks:

* Version uses CalVer, in the format YYYY.MM.DD.HHMMSS e.g. 2026.04.29.155121.
* Consider changesets.
* Configure changelog generation.
* Configure package version bumping.

Acceptance criteria:

* Version changes are explicit.
* Changelog is generated or maintained consistently.

### Story 9.3: Publish Package to npm

Tasks:

* Configure npm publishing token.
* Configure release workflow.
* Publish only from main or tagged releases.
* Support dry-run or preview.
* Ensure package contents are correct.

Acceptance criteria:

* Package can be published to npm.
* Published package contains JS, type definitions, CSS, README, and licence.
* Publishing requires passing tests.

### Story 9.4: Add Release Quality Gates

Tasks:

* Prevent publishing on failing tests.
* Prevent publishing with uncommitted generated files.
* Verify package can be installed in example app.
* Optionally run smoke test after packing.

Acceptance criteria:

* Broken packages cannot be released.
* Release process is repeatable.

# Azure Function GitHub Reference Backlog

## Epic 1: Project Setup

### Story 1.1: Create C# Azure Function Project

Tasks:

* Create Azure Function project.
* Target supported .NET version.
* Use isolated worker model unless there is a reason not to.
* Add project structure.
* Add solution file.
* Add test projects.

Acceptance criteria:

* Function app builds locally.
* Tests run locally.
* Project structure is clear.

### Story 1.2: Configure Local Development

Tasks:

* Add local settings template.
* Document required configuration.
* Add sample repository configuration.
* Add developer setup instructions.

Acceptance criteria:

* Developer can run function locally.
* Secrets are not committed.
* Sample config is safe for public repo.

## Epic 2: Configuration

### Story 2.1: Define Repository Configuration

Tasks:

* Define config model for repositories.
* Define config model for pipelines.
* Support categories such as build and release.
* Support display names.
* Support include/exclude rules if needed.

Acceptance criteria:

* Function can identify which repositories to query.
* Function can map GitHub workflow names to pipeline categories.
* Config can be loaded locally and in Azure.

Example configuration:

```json
{
  "repositories": [
    {
      "owner": "CloudAwesome",
      "name": "example-repo",
      "displayName": "Example Repo",
      "pipelines": [
        {
          "name": "Build",
          "category": "build"
        },
        {
          "name": "Publish",
          "category": "release"
        }
      ]
    }
  ]
}
```

### Story 2.2: Validate Configuration

Tasks:

* Validate required repository fields.
* Validate pipeline mappings.
* Report useful configuration errors.
* Add tests for invalid config.

Acceptance criteria:

* Invalid configuration fails clearly.
* Errors are actionable.

## Epic 3: GitHub Integration

### Story 3.1: Implement GitHub Client

Tasks:

* Choose HTTP client approach.
* Query repository pipeline definitions.
* Query pipeline executions.
* Handle pagination.
* Handle rate limit responses.
* Handle transient failures.

Acceptance criteria:

* GitHub public repository data can be queried.
* Pagination is handled.
* Failures are logged and surfaced safely.

### Story 3.2: Support Public Repository MVP

Tasks:

* Implement unauthenticated calls where possible.
* Add optional token support for higher rate limits.
* Ensure no secrets are exposed in response.

Acceptance criteria:

* Public repositories work without requiring private access.
* Token can be added via configuration if needed.

### Story 3.3: Prepare for Private Repository Support

Tasks:

* Add configuration placeholders for authenticated access.
* Support token from environment or Key Vault.
* Keep auth implementation isolated.
* Document future GitHub App option.

Acceptance criteria:

* Private repo support can be added without changing response contract.
* Auth details do not leak into dashboard data.

## Epic 4: Data Transformation

### Story 4.1: Map GitHub Data to DashboardData

Tasks:

* Map repository data.
* Map GitHub workflows to dashboard pipelines.
* Map GitHub workflow runs to executions.
* Normalize statuses.
* Normalize timestamps.
* Calculate duration.
* Add execution URL.

Acceptance criteria:

* Output matches React package contract.
* GitHub-specific terminology is not exposed in output.
* Mapping logic is unit tested.

### Story 4.2: Handle Unknown or Unmapped Pipelines

Tasks:

* Decide whether unmapped pipelines are excluded or categorized as other.
* Implement behaviour.
* Add configuration option if needed.
* Add tests.

Acceptance criteria:

* Behaviour is documented.
* Unmapped data does not break the response.

### Story 4.3: Status Normalization

Tasks:

* Map GitHub statuses and conclusions to dashboard statuses.
* Handle running and queued executions.
* Handle cancelled executions.
* Handle skipped or neutral results.
* Add tests for all known cases.

Acceptance criteria:

* Status mapping is deterministic.
* Unknown statuses are handled safely.

## Epic 5: Function Endpoint

### Story 5.1: Implement Dashboard Data Endpoint

Tasks:

* Add HTTP-triggered function.
* Return `DashboardData`.
* Add generated timestamp.
* Set JSON content type.
* Add cache headers if appropriate.
* Add error response model.

Acceptance criteria:

* Endpoint returns valid dashboard data.
* Endpoint is consumable by browser clients.
* Errors are clear and safe.

### Story 5.2: Add CORS Support

Tasks:

* Configure CORS for local development.
* Document Azure CORS configuration.
* Ensure Docusaurus/static website can call endpoint.

Acceptance criteria:

* Browser client can call function endpoint.
* CORS setup is documented.

## Epic 6: Caching and Performance

### Story 6.1: Add In-Memory Caching

Tasks:

* Cache dashboard response.
* Configure cache duration.
* Avoid hammering GitHub API.
* Add cache expiry behaviour.
* Add tests using fake clock where appropriate.

Acceptance criteria:

* Repeated calls use cached data.
* Cache expiry triggers refresh.
* Cache behaviour is configurable.

### Story 6.2: Add Resilience Behaviour

Tasks:

* Return stale cache if GitHub fetch fails.
* Include generated timestamp.
* Log refresh failures.
* Consider adding stale indicator metadata.

Acceptance criteria:

* Temporary GitHub failures do not necessarily break dashboard.
* Stale data behaviour is documented.

## Epic 7: Testing

### Story 7.1: Unit Test Configuration

Tasks:

* Test valid config loading.
* Test invalid config.
* Test missing required fields.
* Test pipeline category mapping.

Acceptance criteria:

* Config tests are deterministic.
* Invalid config messages are useful.

### Story 7.2: Unit Test GitHub Mapping

Tasks:

* Test repository mapping.
* Test pipeline mapping.
* Test execution mapping.
* Test status normalization.
* Test duration calculation.
* Test missing optional fields.

Acceptance criteria:

* Mapping layer has high coverage.
* Provider-specific edge cases are covered.

### Story 7.3: Unit Test GitHub Client Behaviour

Tasks:

* Mock HTTP responses.
* Test pagination.
* Test rate limit handling.
* Test error handling.
* Test retry behaviour if implemented.

Acceptance criteria:

* GitHub client does not require live GitHub calls in unit tests.
* Edge cases are covered.

### Story 7.4: Function Endpoint Tests

Tasks:

* Test successful HTTP response.
* Test JSON shape.
* Test error response.
* Test cache behaviour.
* Test CORS-related behaviour if practical.

Acceptance criteria:

* Endpoint behaviour is covered.
* Tests can run in CI without Azure resources.

### Story 7.5: Integration Tests

Tasks:

* Add optional integration test category.
* Query known public repository only when explicitly enabled.
* Keep integration tests out of default CI unless safe.
* Document required environment variables.

Acceptance criteria:

* Integration tests are available.
* Default test suite remains reliable and fast.

## Epic 8: Observability

### Story 8.1: Add Structured Logging

Tasks:

* Log request start and completion.
* Log GitHub calls.
* Log rate limit details where safe.
* Log cache hits and misses.
* Avoid logging secrets.

Acceptance criteria:

* Logs are useful for diagnosis.
* No sensitive values are logged.

### Story 8.2: Add Health Endpoint

Tasks:

* Add simple health endpoint.
* Optionally include config validity.
* Avoid calling GitHub in basic health check.

Acceptance criteria:

* Hosting platform can verify function app is alive.
* Health endpoint does not consume GitHub rate limit.

## Epic 9: Deployment and CI/CD

### Story 9.1: Create Function CI Pipeline

Tasks:

* Restore dependencies.
* Build solution.
* Run tests.
* Publish function artifact.
* Upload build artifact.

Acceptance criteria:

* Pull requests validate function app.
* Main branch produces deployable artifact.
* Tests must pass before artifact creation.

### Story 9.2: Add Deployment Guidance

Tasks:

* Document Azure Function deployment.
* Document required app settings.
* Document CORS configuration.
* Document optional GitHub token configuration.
* Document static website consumption.

Acceptance criteria:

* User can deploy reference implementation from documentation.
* Secrets are configured securely.

### Story 9.3: Optional Deployment Workflow

Tasks:

* Add GitHub Actions workflow for Azure deployment.
* Use publish profile or federated identity.
* Keep deployment optional.
* Document required secrets.

Acceptance criteria:

* Reference app can be deployed automatically.
* Deployment workflow is safe for public repo.

# Cross-Cutting Requirements

## Testing Principles

Testing is a primary requirement.

Both projects should:

* Treat tests as first-class implementation work.
* Prefer deterministic unit tests.
* Avoid live external dependencies in default test runs.
* Include fixtures representing realistic data.
* Enforce coverage thresholds.
* Run tests in CI on every pull request.
* Fail builds on test failures.
* Include edge case coverage.

## CI/CD Principles

The project should be CI/CD-first.

The React package should:

* Validate all pull requests.
* Build on main.
* Publish versioned releases to npm only after passing tests.
* Include type declarations and CSS in package output.
* Use explicit CalVer versioning.

The Azure Function project should:

* Validate all pull requests.
* Build and test on main.
* Produce deployable artifacts.
* Keep deployment separate from validation.
* Avoid requiring Azure resources for unit tests.

## Styling Principles

The React component should:

* Strictly adhere to the 7-1 SCSS architecture.
* Provide useful default CSS.
* Avoid invasive global styles.
* Use stable class names.
* Use CSS variables.
* Allow consumers to fully override appearance.
* Work cleanly inside Docusaurus.

## Platform Neutrality Principles

The React component must avoid:

* GitHub-specific terminology.
* GitHub API assumptions.
* Provider-specific fields.
* Authentication concerns.
* Fetching or polling responsibilities.

Provider-specific details belong in:

* consuming applications
* middleware
* reference implementations
* documentation examples

# Suggested Initial Milestones

## Milestone 1: Contract and Static Rendering

* Define `DashboardData`.
* Create fixtures.
* Render summary cards.
* Render latest pipeline execution table.
* Add basic tests.

## Milestone 2: Filtering and Styling

* Add filters.
* Add default SCSS/CSS.
* Add SCSS/CSS variables.
* Add accessibility checks.
* Add Docusaurus example.

## Milestone 3: Charts and Polish

* Add execution history chart.
* Add richer empty states.
* Add full documentation.
* Enforce coverage thresholds.

## Milestone 4: npm Release

* Configure versioning.
* Configure package publishing.
* Publish first beta release.

## Milestone 5: Azure Function Reference

* Build C# GitHub integration.
* Transform to dashboard contract.
* Add caching.
* Add test suite.
* Document deployment.

# Open Decisions

* Package name: `pipeline-execution-dashboard`
* Repository name: `pipeline-execution-dashboard`
* Whether runtime data validation is included in MVP: Yes, it is.
* Whether charting library should be Recharts or another library: Recharts is acceptable.
* Whether filters should be uncontrolled only for MVP: Yes.
* Whether stale data metadata should be added to the contract.
* Whether Azure Function reference should be in the same repo or separate repo: Prefer completely separate repo.
* Whether GitHub token support is included in the first Azure Function milestone: Doesn't need to be included in the first milestone, but should be considered in the design so as not to require extended refactoring later.

