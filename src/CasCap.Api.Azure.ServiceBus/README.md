# CasCap.Api.Azure.ServiceBus

Helper library for Azure Service Bus. Provides base service classes for queue and topic send/receive operations with event-driven message handling.

## Services / Extensions

| Type | Name | Description |
|------|------|-------------|
| Interface | `IQueueService` | Abstraction for Service Bus queue send and receive operations. |
| Interface | `ITopicService` | Abstraction for Service Bus topic send and receive operations. |
| Service | `ServiceBase` | Abstract base class providing common message and error event handling (`MessageReceivedEvent`, `ErrorReceivedEvent`). |
| Service | `QueueService` | Implements `IQueueService`. Sends single messages, batches, and receives from queues. Supports connection string and `TokenCredential` authentication. |
| Service | `TopicService` | Implements `ITopicService`. Sends single messages, batches, and receives from topics/subscriptions. Supports connection string and `TokenCredential` authentication. |

### Key Methods — `QueueService`

- `SendMessageAsync(ServiceBusMessage)` — Sends a single message to the queue.
- `SendMessageBatchAsync(Queue<ServiceBusMessage>, CancellationToken)` — Sends a batch of messages.
- `ReceiveMessagesAsync(CancellationToken)` — Receives and processes messages from the queue.

### Key Methods — `TopicService`

- `SendMessageToTopicAsync(ServiceBusMessage, CancellationToken)` — Sends a single message to the topic.
- `SendMessageBatchToTopicAsync(Queue<ServiceBusMessage>, CancellationToken)` — Sends a batch of messages.
- `ReceiveFromSubscriptionAsync(CancellationToken)` — Receives and processes messages from a subscription.

## Configuration

No configuration model. Services are constructed directly with connection strings or `TokenCredential`.

## Dependencies

### NuGet Packages

| Package | Description |
|---------|-------------|
| `Azure.Messaging.ServiceBus` | Azure Service Bus client library |
| `CasCap.Common.Logging` | Shared logging infrastructure |
| `CasCap.Common.Extensions` | Common extension methods |

### Project References

None.
