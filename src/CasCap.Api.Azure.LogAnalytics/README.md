# CasCap.Api.Azure.LogAnalytics

Helper library for Azure Log Analytics. Provides a query service for retrieving Application Insights exception data via Azure Monitor Log Analytics, with DI registration and configuration binding.

## Services / Extensions

| Type | Name | Description |
| --- | --- | --- |
| Interface | `IQueryService` | Abstraction for querying Azure Monitor / Application Insights via Log Analytics. |
| Service | `QueryService` | Implements `IQueryService` using `LogsQueryClient`. Authenticates via `TokenCredential`. |
| Extension | `AddCasCapLogAnalyticsServices` | Registers `LogAnalyticsConfig` options and `IQueryService` / `QueryService` with the DI container. |
| Model | `AppInsightsObject` | Record representing a single exception from an Application Insights Log Analytics query. |

### Key Methods

- `GetExceptions(int limit)` — Returns up to `limit` recent exception records from the workspace.
- `Query(QueryTimeRange timeRange)` — Queries the workspace for up to 50 results and outputs to console.

## Configuration

| Class | Section | Properties |
| --- | --- | --- |
| `LogAnalyticsConfig` | `CasCap:LogAnalyticsConfig` | `WorkspaceId` (required) |

## Dependencies

### NuGet Packages

| Package |
| --- |
| [Azure.Identity](https://www.nuget.org/packages/azure.identity) |
| [Azure.Monitor.Query](https://www.nuget.org/packages/azure.monitor.query) |
| [CasCap.Common.Logging](https://www.nuget.org/packages/cascap.common.logging) |
| [CasCap.Common.Extensions](https://www.nuget.org/packages/cascap.common.extensions) |

### Project References

None.
