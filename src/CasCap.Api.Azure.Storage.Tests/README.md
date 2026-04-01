# CasCap.Api.Azure.Storage.Tests

xUnit integration tests for the `CasCap.Api.Azure.Storage` library. Tests cover Blob Storage and Queue Storage operations against an Azurite emulator.

> **Note:** Tests require the Azurite storage emulator running via `docker compose up -d` from the repository root. See the root [README.md](../../README.md) for details.

## Test Classes

| Class | Description |
| --- | --- |
| `TestBase` | Abstract base configuring DI, logging, and Azurite connection string from `appsettings.Test.json`. |
| `AzBlobStorageTests` | Integration tests for `AzBlobStorageBase` (container creation, upload, download, list, delete). |
| `AzQueueStorageTests` | Integration tests for `AzQueueStorageBase` (enqueue, dequeue single/many). |

## Test Services

| Class | Description |
| --- | --- |
| `AzBlobService` | Concrete `AzBlobStorageBase` implementation for test blob operations. |
| `AzQueueService` | Concrete `AzQueueStorageBase` implementation for test queue operations. |
| `TestMessage` | Simple DTO with `Id`, `Dt`, and `TestString` used as a queue message payload. |

## Test Interfaces

| Interface | Description |
| --- | --- |
| `IAzBlobService` | Test-specific blob storage abstraction extending `IAzBlobStorageBase`. |
| `IAzQueueService` | Test-specific queue storage abstraction extending `IAzQueueStorageBase`. |

## Dependencies

### NuGet Packages

| Package |
| --- |
| [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/microsoft.net.test.sdk) |
| [xunit](https://www.nuget.org/packages/xunit) |
| [xunit.runner.visualstudio](https://www.nuget.org/packages/xunit.runner.visualstudio) |
| [coverlet.collector](https://www.nuget.org/packages/coverlet.collector) |
| [coverlet.msbuild](https://www.nuget.org/packages/coverlet.msbuild) |
| [Serilog.Sinks.XUnit](https://www.nuget.org/packages/serilog.sinks.xunit) |
| [Microsoft.Extensions.Configuration.Json](https://www.nuget.org/packages/microsoft.extensions.configuration.json) |
| [CasCap.Common.Logging](https://www.nuget.org/packages/cascap.common.logging) |
| [CasCap.Common.Testing](https://www.nuget.org/packages/cascap.common.testing) |

### Project References

| Project | Description |
| --- | --- |
| `CasCap.Api.Azure.Storage` | The library under test |
