# CasCap.Api.Azure.EventHub

Generic Azure Event Hubs base services for .NET applications. `PublisherService<T>` implements
[`IPublisherService<T>`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.EventHub/Abstractions/IPublisherService%7BT%7D.cs)
and serializes values with MessagePack; `SubscriberService<T>` implements
[`ISubscriberService<T>`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.EventHub/Abstractions/ISubscriberService%7BT%7D.cs)
and receives, deserializes and logs values using blob checkpoints.

## Installation

Targets `net8.0`, `net9.0` and `net10.0`.

```powershell
dotnet add package CasCap.Api.Azure.EventHub
```

## Publishing

Derive a concrete publisher and supply an Event Hubs namespace, hub name and token credential:

```csharp
using Azure.Core;
using CasCap.Services;

public sealed class TextPublisher(
    string fullyQualifiedNamespace, string eventHubName, TokenCredential credential)
    : PublisherService<string>(fullyQualifiedNamespace, eventHubName, credential)
{
}
```

```csharp
var publisher = new TextPublisher(namespaceName, eventHubName, credential);
await publisher.Push("example");
```

`Push(T)` and `Push(List<T>)` use MessagePack; `Push(byte[])` and `Push(List<byte[]>)` send raw
payloads. Use matching serialization on the receiver.
[`IEvent`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.EventHub/Abstractions/IEvent.cs)
exists but is not a generic constraint.
The publisher creates **one SDK batch per call** and throws when a payload collection cannot fit;
it does not split oversized collections automatically.

The connection-string constructor accepts `entityPath`, but the implementation constructs its SDK
client from the connection string alone. Include the hub's `EntityPath` in that string, or prefer the
token-credential overload with an explicit hub name.

## Receiving and Limitations

`SubscriberService<T>` takes an event hub name, Event Hubs connection string, storage connection
string and checkpoint-container name. Its credential overload additionally takes the namespace and
`TokenCredential`, but still requires nonblank connection-string arguments and still uses the storage
connection string for checkpoints.

`InitiateReceive(cancellationToken)` creates the checkpoint container if needed and runs until
cancelled, using the default consumer group. The implementation logs deserialized values and updates
checkpoints periodically. Its event/error handlers are **private**: there is no overridable
application-processing callback or message event. Use the Azure SDK directly when application work
must run before a checkpoint is committed.

The receiver catches outer processing exceptions; returning from `InitiateReceive` is not proof of
successful processing. Information logs can include deserialized event contents. Neither base class
exposes SDK-client disposal through a public disposal contract.

## Authentication and Configuration

Provision the hub and checkpoint storage. Grant publisher send permissions, receiver listen
permissions and checkpoint-storage read/write permissions as needed. Keep connection strings in a
secret provider. There is no options model or automatic DI registration.

## Documentation

- [Publisher implementation](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.EventHub/Services/PublisherService.cs)
- [Subscriber implementation](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.EventHub/Services/SubscriberService.cs)
- [Package catalogue and license](https://github.com/f2calv/CasCap.Api.Azure#packages)
