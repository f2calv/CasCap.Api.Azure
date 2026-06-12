# CasCap.Api.Azure.Storage.Tests

xUnit tests for the `CasCap.Api.Azure.Storage` library. Covers Blob Storage and Queue Storage operations against an Azurite emulator, plus self-contained unit tests for the storage key/date helper extensions.

> **Note:** Integration tests require the Azurite storage emulator running via `docker compose up -d` from the repository root. See the root [README.md](../../README.md) for details. The `AzStorageHelpersTests` unit tests have no external dependency.

## Test Classes

| Class | Methods | Cases | Description |
| --- | --- | --- | --- |
| `AzStorageHelpersTests` | 10 | 18 | Unit tests for `StorageExtensions` key/date helpers (partition key, row key, validation, file-name parsing). |
| `AzBlobStorageTests` | 1 | 1 | Integration test for `AzBlobStorageBase` (container creation, upload, round-trip download). |
| `AzQueueStorageTests` | 1 | 1 | Integration test for `AzQueueStorageBase` (enqueue, dequeue single/many). |
| **Total** | **12** | **20** | |

Test case counts exceed method counts because `[Theory]` methods expand to multiple cases via `[InlineData]`.

## Trait Categories

| Category | Applies To |
| --- | --- |
| `Storage Keys` | `AzStorageHelpersTests` (self-contained unit tests) |
| `Integration` | `AzBlobStorageTests`, `AzQueueStorageTests` (require Azurite) |

## Skipped Tests

None.

## File Structure

```text
Tests/
├── TestBase.cs              # DI + logging + Azurite connection string from appsettings.Test.json
├── AzStorageHelpersTests.cs # Unit tests (no external dependency)
├── AzBlobStorageTests.cs    # Integration tests (Azurite)
├── AzQueueStorageTests.cs   # Integration tests (Azurite)
├── AzBlobService.cs         # Concrete AzBlobStorageBase test helper
├── AzQueueService.cs        # Concrete AzQueueStorageBase test helper
└── TestMessage.cs           # Queue message payload DTO
```

## Test Support Types

| Class | Description |
| --- | --- |
| `TestBase` | Abstract base configuring DI, logging, and Azurite connection string from `appsettings.Test.json`. |
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
