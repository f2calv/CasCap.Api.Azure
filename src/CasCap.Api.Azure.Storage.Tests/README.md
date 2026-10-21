# CasCap.Api.Azure.Storage.Tests

xUnit v3 tests for Azure Blob and Queue helpers against Azurite, plus offline storage key/date tests.
The Microsoft.Testing.Platform executable targets .NET 8, .NET 9 and .NET 10.

## Tests

Counts are source-declared methods and expanded `InlineData` cases, **per target framework**.
They are not observed execution results.

| Class | Methods | Expanded cases | Category | Scope |
| --- | ---: | ---: | --- | --- |
| `AzStorageHelpersTests` | 10 | 18 | `Storage Keys` | Partition/row keys, validation and file-name dates |
| `AzBlobStorageTests` | 1 | 1 | `Integration` | Container creation, upload and round-trip download |
| `AzQueueStorageTests` | 1 | 1 | `Integration` | Enqueue and destructive dequeue |
| **Total** | **12** | **20** | | |

These are all the trait categories. There is no Azure Table integration test in this project.

## Skipped Tests

No declared skips. Integration tests do **not** skip when Azurite is absent; they fail to connect.

## Prerequisites and Execution

Use a stable .NET 10 SDK and install runtimes for the target frameworks you select.
Integration tests read `ConnectionStrings:storage` from `appsettings.Test.json` and require the
repository's Azurite Compose environment, with Blob/Queue/Table ports 10000–10002 available.
The test fixture does not load user secrets or environment-variable configuration.

Start the existing repository Compose definition only when you intend to run integration tests.
They create containers and queues, upload data and delete dequeued messages. Keep the supplied
emulator configuration; never substitute a shared production storage account.

From the repository root, after explicit approval:

```powershell
dotnet test --project .\src\CasCap.Api.Azure.Storage.Tests\CasCap.Api.Azure.Storage.Tests.csproj --configuration Release --filter-not-trait Category=Integration
```

Omit the filter only when Azurite is running and integration execution is approved. Release consumes
published shared dependencies; Debug additionally needs an adjacent
[CasCap.Common checkout](https://github.com/f2calv/CasCap.Common).

## Layout

```text
Tests/
├── TestBase.cs
├── AzStorageHelpersTests.cs
├── AzBlobStorageTests.cs
├── AzQueueStorageTests.cs
├── AzBlobService.cs
├── AzQueueService.cs
└── TestMessage.cs
```

`TestBase` builds the configuration and service container. `AzBlobService` and `AzQueueService`
provide concrete test implementations of the library base classes; `TestMessage` is the queue
payload. Test-specific interfaces live under `Abstractions/`.

See the [Storage package README](../CasCap.Api.Azure.Storage/README.md) for consumer APIs and
queue-delivery cautions.
