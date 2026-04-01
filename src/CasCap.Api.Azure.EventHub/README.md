# CasCap.Api.Azure.EventHub

Helper library for Azure Event Hub. Provides generic publisher and subscriber base services for streaming events via Event Hubs, with MessagePack serialization.

## Services / Extensions

| Type | Name | Description |
| --- | --- | --- |
| Interface | `IEvent` | Marker interface for Event Hub event objects. |
| Interface | `IPublisherService<T>` | Abstraction for publishing messages to an Event Hub. |
| Interface | `ISubscriberService<T>` | Abstraction for receiving and processing messages from an Event Hub. |
| Service | `PublisherService<T>` | Abstract base implementing `IPublisherService<T>`. Serializes events via MessagePack and sends them in batches. Supports connection string and `TokenCredential` authentication. |
| Service | `SubscriberService<T>` | Abstract base implementing `ISubscriberService<T>`. Uses `EventProcessorClient` with blob checkpoint storage for reliable event processing. Supports connection string and `TokenCredential` authentication. |

### Key Methods — `PublisherService<T>`

- `Push(T obj)` — Serializes and pushes a single event.
- `Push(List<T> objs)` — Serializes and pushes a list of events.
- `Push(byte[] bytes)` — Pushes a raw byte array as a single event.
- `SendTestMessages(int numMessagesToSend)` — Sends test messages to the Event Hub.

### Key Methods — `SubscriberService<T>`

- `InitiateReceive(CancellationToken)` — Begins processing events until cancellation.

## Configuration

No configuration model. Services are constructed directly with connection strings or `TokenCredential`.

## Dependencies

### NuGet Packages

| Package |
| --- |
| [Azure.Messaging.EventHubs](https://www.nuget.org/packages/azure.messaging.eventhubs) |
| [Azure.Messaging.EventHubs.Processor](https://www.nuget.org/packages/azure.messaging.eventhubs.processor) |
| [CasCap.Common.Logging](https://www.nuget.org/packages/cascap.common.logging) |
| [CasCap.Common.Extensions](https://www.nuget.org/packages/cascap.common.extensions) |
| [CasCap.Common.Serialization.MessagePack](https://www.nuget.org/packages/cascap.common.serialization.messagepack) |

### Project References

None.
