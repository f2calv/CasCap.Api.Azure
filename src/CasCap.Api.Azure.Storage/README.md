# CasCap.Api.Azure.Storage

Helper library for Azure Storage Services. Provides abstract base service classes for Blob, Queue, and Table storage operations with both connection string and `TokenCredential` authentication.

## Installation

```bash
dotnet add package CasCap.Api.Azure.Storage
```

## Services / Extensions

| Type | Name | Description |
| --- | --- | --- |
| Interface | [`IAzBlobStorageBase`](Abstractions/IAzBlobStorageBase.cs) | Contract for Azure Blob Storage container operations (upload, download, list, delete). |
| Interface | [`IAzQueueStorageBase`](Abstractions/IAzQueueStorageBase.cs) | Contract for Azure Queue Storage operations (enqueue, dequeue). |
| Interface | [`IAzTableStorageBase`](Abstractions/IAzTableStorageBase.cs) | Contract for Azure Table Storage CRUD operations with batch support. |
| Service | [`AzBlobStorageBase`](Services/Base/AzBlobStorageBase.cs) | Abstract base implementing `IAzBlobStorageBase`. Supports block blobs and append blobs. |
| Service | [`AzQueueStorageBase`](Services/Base/AzQueueStorageBase.cs) | Abstract base implementing `IAzQueueStorageBase`. Handles Base64 message encoding and JSON serialization. |
| Service | [`AzTableStorageBase`](Services/Base/AzTableStorageBase.cs) | Abstract base implementing `IAzTableStorageBase`. Supports batch upsert, delete, and query with parallel partition processing. |
| Extension | [`StorageExtensions`](Extensions/StorageExtensions.cs) | Extension methods for Table Storage key validation (`IsKeyValid`), `TableServiceClient.ExistsAsync`, and date-from-filename parsing. |
| Message | [`AzTableStorageArgs`](Messages/AzTableStorageArgs.cs) | Event args for `BatchCompletedEvent`, carrying batch metadata (table name, partition key, count, remaining). |

### Key Methods — [`AzBlobStorageBase`](Services/Base/AzBlobStorageBase.cs)

- `CreateContainerIfNotExists(CancellationToken)` — Creates the blob container if it does not exist.
- `DownloadBlobAsync(string blobName, CancellationToken)` — Downloads blob content as a byte array.
- `ListContainerBlobs(string? prefix, CancellationToken)` — Lists blobs in the container.
- `GetBlobPrefixes(string? prefix, CancellationToken)` — Retrieves virtual directory prefixes.
- `DeleteBlob(string blobName, CancellationToken)` — Deletes a blob.
- `UploadBlob(string blobName, byte[] | Stream, CancellationToken)` — Uploads blob content.

### Key Methods — [`AzQueueStorageBase`](Services/Base/AzQueueStorageBase.cs)

- `Enqueue<T>(T obj)` / `Enqueue<T>(List<T> objs)` — Serializes and enqueues messages.
- `DequeueSingle<T>()` — Dequeues and deserializes a single message.
- `DequeueMany<T>(int limit)` — Dequeues and deserializes multiple messages.

### Key Methods — [`AzTableStorageBase`](Services/Base/AzTableStorageBase.cs)

- `GetTables(CancellationToken)` — Lists all tables in the storage account.
- `GetTableClient(string tableName, bool CreateIfNotExists, CancellationToken)` — Gets a `TableClient` for a table.
- `UploadData<T>(TableClient, List<T>, bool useParallelism, CancellationToken)` — Batch upsert entities (thread-safe via `ConcurrentBag<T>`).
- `UpsertEntity<T>(string tableName, T entity, CancellationToken)` — Upserts a single entity.
- `DeleteData<T>(TableClient, List<T>, CancellationToken)` — Batch delete entities.
- `GetEntities<T>(TableClient, ...)` — Queries entities with optional filtering and paging.

## Class Hierarchy

Abstract base classes for Azure Storage services:

```mermaid
classDiagram
    direction TB

    IAzBlobStorageBase <|.. AzBlobStorageBase
    IAzQueueStorageBase <|.. AzQueueStorageBase
    IAzTableStorageBase <|.. AzTableStorageBase

    AzBlobStorageBase <|-- YourBlobService
    AzQueueStorageBase <|-- YourQueueService
    AzTableStorageBase <|-- YourTableService

    class IAzBlobStorageBase {
        <<interface>>
        +CreateContainerIfNotExists() Task
        +DownloadBlobAsync(name) Task~byte[]~
        +ListContainerBlobs(prefix) IAsyncEnumerable~BlobItem~
        +DeleteBlob(name) Task
    }

    class AzBlobStorageBase {
        <<abstract>>
        #BlobContainerClient _containerClient
        #ILogger Logger
        +CreateContainerIfNotExists() Task
        +DownloadBlobAsync(name) Task~byte[]~
        +UploadBlob(name, data) Task
        +ListContainerBlobs(prefix) Task~List~BlobItem~~
        +GetBlobPrefixes(prefix) Task~List~string~~
    }

    class IAzQueueStorageBase {
        <<interface>>
        +Enqueue~T~(obj) Task
        +DequeueSingle~T~() Task~T~
        +DequeueMany~T~(limit) Task~List~T~~
    }

    class AzQueueStorageBase {
        <<abstract>>
        #QueueClient _queueClient
        #ILogger Logger
        +Enqueue~T~(obj) Task~bool~
        +Enqueue~T~(objs) Task~bool~
        +DequeueSingle~T~() Task~T~
        +DequeueMany~T~(limit) Task~List~T~~
    }

    class IAzTableStorageBase {
        <<interface>>
        +GetTables() AsyncPageable~TableItem~
        +UpsertEntity~T~(table, entity) Task
        +GetEntities~T~(table) AsyncPageable~T~
    }

    class AzTableStorageBase {
        <<abstract>>
        #TableServiceClient _tableSvcClient
        #ILogger Logger
        +event BatchCompletedEvent
        +GetTables() AsyncPageable~TableItem~
        +UploadData~T~(client, entities, parallel) Task
        +UpsertEntity~T~(table, entity) Task
        +DeleteData~T~(client, entities) Task
        +GetEntities~T~(client, filter) Task~List~T~~
    }

    class YourBlobService {
        +UploadImage(bytes) Task
        +GetImage(id) Task~byte[]~
    }

    class YourQueueService {
        +EnqueueMessage(msg) Task
        +ProcessMessages() Task
    }

    class YourTableService {
        +SaveEntity(entity) Task
        +QueryEntities(filter) Task~List~
    }

    AzBlobStorageBase ..> BlobContainerClient : uses
    AzQueueStorageBase ..> QueueClient : uses
    AzTableStorageBase ..> TableServiceClient : uses
```

**Usage Pattern:**

1. Inherit from the appropriate abstract base class
2. Pass connection string or `TokenCredential` (and optional `ServiceVersion`) to base constructor
3. Use protected client and logger fields
4. Call base methods or add domain-specific operations

## Configuration

No `IAppConfig` model — services are constructed directly via their base class constructors.

All three base classes accept an optional `ServiceVersion?` parameter to pin the Azure Storage API version (useful for compatibility with [Azurite](https://github.com/Azure/Azurite) or older service endpoints):

```csharp
// Pin to a specific Blob API version (e.g. for Azurite compatibility)
public class MyBlobService(string connectionString, string containerName)
    : AzBlobStorageBase(connectionString, containerName, BlobClientOptions.ServiceVersion.V2024_11_04)
{
}

// Use SDK-default (latest) version — omit or pass null
public class MyTableService(string connectionString)
    : AzTableStorageBase(connectionString)
{
}
```

When `null` (the default), each SDK uses its built-in latest `ServiceVersion`.

## Dependencies

### NuGet Packages

| Package |
| --- |
| [Azure.Core](https://www.nuget.org/packages/azure.core) |
| [Azure.Storage.Blobs](https://www.nuget.org/packages/azure.storage.blobs) |
| [Azure.Storage.Queues](https://www.nuget.org/packages/azure.storage.queues) |
| [Azure.Data.Tables](https://www.nuget.org/packages/azure.data.tables) |
| [System.Linq.Async](https://www.nuget.org/packages/system.linq.async) |
| [CasCap.Common.Logging](https://www.nuget.org/packages/cascap.common.logging) |
| [CasCap.Common.Extensions](https://www.nuget.org/packages/cascap.common.extensions) |
| [CasCap.Common.Serialization.Json](https://www.nuget.org/packages/cascap.common.serialization.json) |

### Project References

None.
