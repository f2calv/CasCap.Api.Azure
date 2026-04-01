# CasCap.Api.Azure.AppInsights

Helper library for Azure Application Insights. Provides configuration binding and DI registration for Application Insights instrumentation.

## Services / Extensions

| Type | Name | Description |
| --- | --- | --- |
| Extension | `AddCasCapAppInsightsServices` | Registers `AppInsightsConfig` options from the `CasCap:AppInsightsConfig` configuration section. |

## Configuration

| Class | Section | Properties |
| --- | --- | --- |
| `AppInsightsConfig` | `CasCap:AppInsightsConfig` | `InstrumentationKey` (required) |

## Dependencies

### NuGet Packages

| Package |
| --- |
| [CasCap.Common.Logging](https://www.nuget.org/packages/cascap.common.logging) |
| [CasCap.Common.Extensions](https://www.nuget.org/packages/cascap.common.extensions) |

### Project References

None.
