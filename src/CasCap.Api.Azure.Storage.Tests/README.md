# CasCap.Api.Azure.Storage.Tests

xUnit integration tests for the `CasCap.Api.Azure.Storage` library. Tests cover Blob Storage and Queue Storage operations against an Azurite emulator.

> **Note:** Tests require the Azurite storage emulator running via `docker compose up -d` from the repository root. See the root [README.md](../../README.md) for details.

## Test Classes

| Class | Description |
| ----- | ----------- |
| `TestBase` | Abstract base configuring DI, logging, and Azurite connection string from `appsettings.Test.json`. |
| `AzBlobStorageTests` | Integration tests for `AzBlobStorageBase` (container creation, upload, download, list, delete). |
| `AzQueueStorageTests` | Integration tests for `AzQueueStorageBase` (enqueue, dequeue single/many). |

## Test Services

| Class | Description |
| ----- | ----------- |
| `AzBlobService` | Concrete `AzBlobStorageBase` implementation for test blob operations. |
| `AzQueueService` | Concrete `AzQueueStorageBase` implementation for test queue operations. |
| `TestMessage` | Simple DTO with `Id`, `Dt`, and `TestString` used as a queue message payload. |

## Test Interfaces

| Interface | Description |
| --------- | ----------- |
| `IAzBlobService` | Test-specific blob storage abstraction extending `IAzBlobStorageBase`. |
| `IAzQueueService` | Test-specific queue storage abstraction extending `IAzQueueStorageBase`. |

## Dependencies

### NuGet Packages

| Package | Description |
| ------- | ----------- |
| `Microsoft.NET.Test.Sdk` | .NET test SDK infrastructure |
| `xunit` | xUnit testing framework |
| `xunit.runner.visualstudio` | xUnit Visual Studio test runner |
| `coverlet.collector` | Code coverage collector |
| `coverlet.msbuild` | Code coverage MSBuild integration |
| `Serilog.Sinks.XUnit` | Serilog sink for xUnit test output |
| `Microsoft.Extensions.Configuration.Json` | JSON configuration file provider |
| `CasCap.Common.Logging` | Shared logging infrastructure |
| `CasCap.Common.Testing` | Shared test utilities |

### Project References

| Project | Description |
| ------- | ----------- |
| `CasCap.Api.Azure.Storage` | The library under test |
