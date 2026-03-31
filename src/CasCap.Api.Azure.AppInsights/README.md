# CasCap.Api.Azure.AppInsights

Helper library for Azure Application Insights. Provides configuration binding and DI registration for Application Insights instrumentation.

## Services / Extensions

| Type | Name | Description |
| ---- | ---- | ----------- |
| Extension | `AddCasCapAppInsightsServices` | Registers `AppInsightsConfig` options from the `CasCap:AppInsightsConfig` configuration section. |

## Configuration

| Class | Section | Properties |
| ----- | ------- | ---------- |
| `AppInsightsConfig` | `CasCap:AppInsightsConfig` | `InstrumentationKey` (required) |

## Dependencies

### NuGet Packages

| Package | Description |
| ------- | ----------- |
| `CasCap.Common.Logging` | Shared logging infrastructure |
| `CasCap.Common.Extensions` | Common extension methods |

### Project References

None.
