# CasCap.Api.Azure.Storage

Abstract Azure Blob, Queue and Table Storage services for .NET applications. Derive a concrete
service to reuse the corresponding
[`IAzBlobStorageBase`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.Storage/Abstractions/IAzBlobStorageBase.cs),
[`IAzQueueStorageBase`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.Storage/Abstractions/IAzQueueStorageBase.cs)
or [`IAzTableStorageBase`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.Storage/Abstractions/IAzTableStorageBase.cs) implementation.

## Installation

Targets `net8.0`, `net9.0` and `net10.0`.

```powershell
dotnet add package CasCap.Api.Azure.Storage
```

## Usage

Supply a connection string from secret-backed application configuration:

```csharp
using CasCap.Services;

public sealed class DocumentStorage(string connectionString)
    : AzBlobStorageBase(connectionString, "documents")
{
}
```

```csharp
var storage = new DocumentStorage(connectionString);
await storage.UploadBlob("example.txt", "Hello"u8.ToArray(), cancellationToken);
byte[]? bytes = await storage.DownloadBlobAsync("example.txt", cancellationToken);
```

The connection-string and URI/credential blob constructors synchronously create the container if it
does not exist, so constructing or resolving them performs network I/O and requires creation
permission when the container is absent. Uploads overwrite existing block blobs.
`DownloadBlobAsync` returns `null` for a missing blob and buffers its entire contents.

### Conditional Streaming Reads

The following API is local source work, not yet a published package release. Pass an existing
`BlobContainerClient` to avoid constructor-time network I/O and container provisioning, while
preserving the caller's identity, transport, retry and service-version settings:

```csharp
using Azure;
using Azure.Storage.Blobs;
using CasCap.Services;

public sealed class ExistingDocuments(BlobContainerClient client) : AzBlobStorageBase(client);
```

```csharp
var storage = new ExistingDocuments(containerClient);
await using var stream = await storage.OpenBlobReadAsync(
    "example.pdf", new ETag(admittedETag), cancellationToken);
await ProcessBoundedAsync(stream, cancellationToken);
```

`OpenBlobReadAsync` requires one exact, strong entity tag: no missing tag, wildcard, weak tag or
tag list. It uses the SDK's non-modifiable read stream with `If-Match` on the initial metadata
request and subsequent range reads. A changed source fails with Azure status 412, including after
reading has started; no newer revision is silently substituted. Missing sources and other Azure
failures also propagate without an existence probe or a success-shaped fallback.

The caller owns disposal and must pass cancellation to subsequent reads as well as opening.
The SDK uses a 64 KiB read buffer and can read ahead; this is not a total download-size limit.
Apply application-specific byte/page/content limits while consuming the stream. The streaming path
does not log source names or document contents.

| Base service | Selected operations | Important behavior |
| --- | --- | --- |
| `AzBlobStorageBase` | `UploadBlob`, `DownloadBlobAsync`, `OpenBlobReadAsync`, `ListContainerBlobs`, `GetBlobPrefixes`, `DeleteBlob` | Conditional streaming reads are available in local source; lists are materialized |
| `AzQueueStorageBase` | `Enqueue<T>`, `DequeueSingle<T>`, `DequeueMany<T>` | JSON with Base64 queue encoding; creates the queue on first operation |
| `AzTableStorageBase` | `GetTables`, `GetTableClient`, `UpsertEntity`, `UploadData`, `DeleteData`, `GetEntities` | Batch operations and `BatchCompletedEvent`; queries return materialized collections |
| `StorageExtensions` | `IsKeyValid`, table existence and date/key conversion helpers | Utility extensions, not service registration |

### Queue Delivery Caution

Dequeue methods deserialize and **delete messages before returning them**, not after your
application processes them. `DequeueSingle<T>` also deletes malformed JSON messages and returns a
null object with the message metadata. Use the Azure SDK directly when you need explicit
acknowledgement, poison-message handling or processing retries.

`Enqueue(List<T>)` returns `true` when **at least one** send succeeds; it is not an all-items-success
guarantee. Deserialization failures can log payload content, so configure log destinations accordingly.

## Authentication and Service Versions

All three base classes have connection-string and `TokenCredential` constructors. The additional
Blob client constructor above does not provision resources. Token constructors
take the appropriate service URI; Blob and Queue also take a container/queue name, which is appended
to that URI. Table takes a table-service endpoint. Grant the caller the storage data-plane roles
needed for its reads, writes and resource creation.

There is no options model or DI registration helper. Each base constructor accepts an optional SDK
`ServiceVersion?`, useful when an emulator supports an older service API:

```csharp
using Azure.Storage.Blobs;
using CasCap.Services;

public sealed class EmulatorDocuments(string connectionString)
    : AzBlobStorageBase(
        connectionString, "documents", BlobClientOptions.ServiceVersion.V2024_11_04)
{
}
```

Omitting the version uses that SDK's default; it does not negotiate emulator compatibility.
Store connection strings outside tracked configuration.

## Documentation

- [Blob contract](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.Storage/Abstractions/IAzBlobStorageBase.cs)
- [Queue contract](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.Storage/Abstractions/IAzQueueStorageBase.cs)
- [Table contract](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.Storage/Abstractions/IAzTableStorageBase.cs)
- [Storage test prerequisites and inventory](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.Storage.Tests/README.md)
- [Package catalogue and license](https://github.com/f2calv/CasCap.Api.Azure#packages)
