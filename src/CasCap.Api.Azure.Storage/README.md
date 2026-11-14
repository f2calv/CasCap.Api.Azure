# CasCap.Api.Azure.Storage

Abstract Azure Blob, Queue and Table Storage services for .NET applications. Derive a concrete
service to reuse the corresponding
[`IAzBlobStorageBase`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.Storage/Abstractions/IAzBlobStorageBase.cs),
[`IAzQueueStorageBase`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.Storage/Abstractions/IAzQueueStorageBase.cs)
or [`IAzTableStorageBase`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.Storage/Abstractions/IAzTableStorageBase.cs) implementation.

## Installation

Targets `net10.0` and `net11.0`.

```powershell
dotnet add package CasCap.Api.Azure.Storage
```

## Migrating to 2.0

[`IAzBlobStorageBase`](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.Storage/Abstractions/IAzBlobStorageBase.cs)
adds two required members in 2.0. Direct implementers and test doubles must add:

- `Task<bool> UploadBlobIfNotExistsAsync(string blobName, Stream stream, AzBlobUploadArgs? args = null, CancellationToken cancellationToken = default)`
- `Task<IDictionary<string, string>?> GetBlobMetadataAsync(string blobName, CancellationToken cancellationToken = default)`

Subclasses of `AzBlobStorageBase` inherit both implementations and need no additional members.
Use the create-only method when overwriting is forbidden; existing `UploadBlob` overloads continue
to overwrite. Callers still own the upload stream. See the behavior and example below.

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

Pass an existing `BlobContainerClient` to avoid constructor-time network I/O and container provisioning, while
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

### Create-Only Uploads

Back up or archive content without ever replacing an existing blob:

```csharp
using Azure.Storage.Blobs.Models;
using CasCap.Messages;

var args = new AzBlobUploadArgs
{
    ContentType = "image/jpeg",
    Metadata = new Dictionary<string, string> { ["sha256"] = sha256 },
    AccessTier = AccessTier.Cool,
};
await using var content = File.OpenRead(path);
bool created = await storage.UploadBlobIfNotExistsAsync("photos/example.jpg", content, args, cancellationToken);
IDictionary<string, string>? metadata = await storage.GetBlobMetadataAsync("photos/example.jpg", cancellationToken);
```

`UploadBlobIfNotExistsAsync` streams the content with `If-None-Match: *` and returns `false`, leaving
the blob unchanged, when one already exists, including one created concurrently. A missing container,
authorization failure and other Azure errors propagate. `GetBlobMetadataAsync` returns `null` for a
missing blob. Neither method logs blob names or metadata.

| Base service | Selected operations | Important behavior |
| --- | --- | --- |
| `AzBlobStorageBase` | `UploadBlob`, `UploadBlobIfNotExistsAsync`, `GetBlobMetadataAsync`, `DownloadBlobAsync`, `OpenBlobReadAsync`, `ListContainerBlobs`, `GetBlobPrefixes`, `DeleteBlob` | `UploadBlob` overwrites; create-only uploads never overwrite; metadata returns `null` for a missing blob; conditional reads bind one revision; lists are materialized |
| `AzQueueStorageBase` | `Enqueue<T>`, `DequeueSingle<T>`, `DequeueMany<T>` | JSON with Base64 queue encoding; creates the queue on first operation |
| `AzTableStorageBase` | `GetTables`, `GetTableClient`, `UpsertEntity`, `UploadData`, `DeleteData`, `GetEntities` | Batch operations and `BatchCompletedEvent`; queries return materialized collections |
| `StorageExtensions` | `IsKeyValid`, table existence and date/key conversion helpers | Utility extensions, not service registration |

### Queue Delivery Caution

Dequeue methods deserialize and **delete messages before returning them**, not after your
application processes them. `DequeueSingle<T>` also deletes malformed JSON messages and returns a
null object with the message metadata. Use the Azure SDK directly when you need explicit
acknowledgement, poison-message handling or processing retries.

`Enqueue(List<T>)` returns `true` when **at least one** send succeeds; it is not an all-items-success
guarantee. A deserialization failure logs one Error with the message type, message identifier and
queue name, never the payload. Blob names, prefixes and table partition keys are not logged; a
missing blob in `DownloadBlobAsync` logs at Debug and returns `null`, and table batch failures
propagate without being logged.

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
