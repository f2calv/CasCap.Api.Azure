# CasCap.Api.Azure.AppInsights

Configuration binding for .NET applications that use Azure Application Insights. This package exposes
[`AppInsightsConfig`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.AppInsights/Models/_AppInsightsConfig.cs)
and its options registration; it does **not** register an Application Insights SDK,
collect telemetry or configure a telemetry exporter.

## Installation

Targets `net8.0`, `net9.0` and `net10.0`.

```powershell
dotnet add package CasCap.Api.Azure.AppInsights
```

## Usage

Register configuration and options in your application's service collection:

```csharp
using CasCap.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

services.AddSingleton<IConfiguration>(configuration);
services.AddOptions();
services.AddCasCapAppInsightsServices();
```

Consumers can inject `IOptions<AppInsightsConfig>` and read `Value.InstrumentationKey`. The extension
binds `CasCap:AppInsightsConfig` using `IConfiguration`; it does not add startup validation.
Configure actual telemetry collection separately using the SDK appropriate to your application.

## Configuration Examples

The options model has one required property:

```json
{
  "CasCap": {
    "AppInsightsConfig": {
      "InstrumentationKey": "00000000-0000-0000-0000-000000000000"
    }
  }
}
```

Supply your own resource's setting through configuration. This model exposes an instrumentation key,
not an Application Insights connection string. For telemetry ingestion, follow Microsoft's
[connection-string guidance](https://learn.microsoft.com/en-us/azure/azure-monitor/app/connection-strings)
and configure the chosen telemetry SDK separately. Service guidance checked **20 October 2026**.

## Documentation

- [Options registration source](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.AppInsights/Extensions/ServiceCollectionExtensions.cs)
- [Package catalogue and license](https://github.com/f2calv/CasCap.Api.Azure#packages)
