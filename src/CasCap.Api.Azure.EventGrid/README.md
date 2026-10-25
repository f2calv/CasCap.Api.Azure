# CasCap.Api.Azure.EventGrid

Azure Event Grid dependencies and Blob Storage notification parsing for .NET consumers targeting
`net8.0`, `net9.0` and `net10.0`. The parser is new in this source checkout; its availability in a
published package has not been established.

## Installation

```powershell
dotnet add package CasCap.Api.Azure.EventGrid
```

The existing package supplies `Azure.Messaging.EventGrid` and shared logging/extension dependencies.
Use a project reference to this checkout for the new parsing API until its release is verified.
There is no publisher, subscriber, configuration or registration API. For delivery clients, use the
Azure SDK and follow its
[Event Grid client guidance](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.eventgrid-readme).

## Parse Blob Notifications

[`BlobStorageEventReader`](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.EventGrid/Services/BlobStorageEventReader.cs)
reads a single Event Grid-schema notification, including a singleton array, into
[`BlobStorageEvent`](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.EventGrid/Models/BlobStorageEvent.cs).
These source links refer to new files awaiting publication, not verified default-branch APIs.

```csharp
using CasCap.Services;

// body is the received BinaryData envelope; no queue acknowledgement occurs here.
var notification = BlobStorageEventReader.Read(body);
```

Only `Microsoft.Storage.BlobCreated` and `Microsoft.Storage.BlobDeleted` are supported.
The parser requires bounded event identity/topic/subject fields, a credential-free HTTPS blob URI
and a hexadecimal sequencer of at most 128 characters. It normalizes sequencers to uppercase,
left-padded to 128 characters for **per-blob** ordinal ordering; do not compare different blobs.
Malformed/empty or unsupported input throws rather than returning a success-shaped notification.
An optional ETag is bounded to 200 characters.

Parsing does not authorize a source, download or validate PDF contents, elect a worker, commit a
job or acknowledge delivery. Consumers must allowlist topic/container/subject and bind trusted
application scope themselves. Keep source bodies and parser exceptions out of exported telemetry:
SDK errors can contain input. No live Azure access is needed to parse an envelope.

## Documentation

- [Project contents](https://github.com/f2calv/CasCap.Api.Azure/tree/main/src/CasCap.Api.Azure.EventGrid)
- [Package catalogue and license](https://github.com/f2calv/CasCap.Api.Azure#packages)
