# CasCap.Api.Azure.LogAnalytics

Azure Monitor Log Analytics helpers for .NET applications that query Application Insights data.
[`IQueryService`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.LogAnalytics/Abstractions/IQueryService.cs)
exposes two fixed queries, not a general-purpose Kusto query interface.

## Installation

Targets `net8.0`, `net9.0` and `net10.0`.

```powershell
dotnet add package CasCap.Api.Azure.LogAnalytics
```

## Usage

Register the application's configuration, logger infrastructure and an authorized `TokenCredential`
before registering this package:

```csharp
using Azure.Core;
using CasCap.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

services.AddSingleton<IConfiguration>(configuration);
services.AddLogging();
services.AddSingleton<TokenCredential>(credential);
services.AddCasCapLogAnalyticsServices();
```

Consumers can inject `IQueryService`:

```csharp
var exceptions = await queryService.GetExceptions(limit: 20);
```

- `GetExceptions(limit = 50)` queries the `exceptions` table over the last day and maps rows to
  `AppInsightsObject`. Its query applies `limit` before ordering by `timestamp`; it does not
  guarantee the newest records across the entire window.
- `Query(QueryTimeRange)` runs `union * | limit 50 | order by timestamp` and logs returned values.
  Use it only where logs may safely contain workspace data.

Both methods require the table and column names expected by the implementation. Workspaces using
different schemas need a different query client. Neither method accepts a cancellation token.

## Configuration Examples

`LogAnalyticsConfig.WorkspaceId` is required and binds from `CasCap:LogAnalyticsConfig`.
Registration binds options but does not add startup validation.

```json
{
  "CasCap": {
    "LogAnalyticsConfig": {
      "WorkspaceId": "00000000-0000-0000-0000-000000000000"
    }
  }
}
```

Replace the placeholder with your workspace ID through your application's configuration. Grant the
credential permission to query that workspace, such as an appropriate Log Analytics read role.
This package does not acquire credentials or configure workspace access.

## Documentation

- [Query contract](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.LogAnalytics/Abstractions/IQueryService.cs)
- [Query implementation and expected schema](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.LogAnalytics/Services/QueryService.cs)
- [Package catalogue and license](https://github.com/f2calv/CasCap.Api.Azure#packages)
