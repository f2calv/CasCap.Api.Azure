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
| `AzBlobConditionalReadTests` | 6 | 12 | `Blob Conditional Read` | Offline SDK HTTP pipeline: non-provisioning construction, conditional metadata/ranges, revision changes during reading, failures, validation, cancellation and stream ownership |
| **Total** | **18** | **32** | | |

These are all the trait categories. There is no Azure Table integration test in this project.

The conditional-read cases use synthetic responses through an in-memory HTTP handler; they do not
start Azurite or contact Azure. They verify SDK request behavior, not live identity, RBAC, networking
or emulator compatibility. Run only this group from the repository root:

```powershell
dotnet test --project .\src\CasCap.Api.Azure.Storage.Tests --configuration Debug --framework net10.0 --filter-class '*AzBlobConditionalReadTests'
```

Observed focused Debug result on .NET 10: **12 passed, zero failed/skipped**. The library also built
for all three target frameworks with zero warnings/errors. No integration or full-suite result is
claimed.

## Skipped Tests

No declared skips. Integration tests do **not** skip when Azurite is absent; they fail to connect.

## Prerequisites and Execution

Use a stable .NET 10 SDK and install runtimes for the target frameworks you select.
Integration tests read `ConnectionStrings:storage` through the shared `Test`-environment configuration
chain: base settings, optional [appsettings.Test.json](appsettings.Test.json) and local overrides,
User Secrets, then environment variables. The test file may be absent when another provider supplies
the required value. Integration tests require the repository's
[Azurite Compose environment](../../docker-compose.yml).

Start the existing repository Compose definition only when you intend to run integration tests.
They create containers and queues, upload data and delete dequeued messages. Keep the supplied
emulator configuration; never substitute a shared production storage account.

```powershell
docker compose up -d --wait azurite
```

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
├── Unit/
│   └── AzBlobConditionalReadTests.cs
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
