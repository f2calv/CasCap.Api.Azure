# CasCap.Api.Azure.ServiceBus

Azure Service Bus queue and topic helpers for .NET applications. `QueueService` and `TopicService`
send individual messages or batches and expose synchronous message/error events while receiving.

## Installation

Targets `net8.0`, `net9.0` and `net10.0`.

```powershell
dotnet add package CasCap.Api.Azure.ServiceBus
```

## Usage

Supply a logger and either a connection string or a namespace and `TokenCredential`:

```csharp
using Azure.Messaging.ServiceBus;
using CasCap.Services;

await using var queue = new QueueService(logger, namespaceName, queueName, credential);
await queue.SendMessageAsync(new ServiceBusMessage("example"), cancellationToken);
```

The application must provision the queue first and grant the credential send/listen access for the
operations it performs. Constructors do not create queues, topics or subscriptions.

| Service | Send | Receive |
| --- | --- | --- |
| `QueueService` / [`IQueueService`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.ServiceBus/Abstractions/IQueueService.cs) | `SendMessageAsync`, `SendMessageBatchAsync` | `ReceiveMessagesAsync` |
| `TopicService` / [`ITopicService`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.ServiceBus/Abstractions/ITopicService.cs) | `SendMessageToTopicAsync`, `SendMessageBatchToTopicAsync` | `ReceiveMessagesFromSubscriptionAsync` |

`TopicService` additionally requires a topic name and subscription name. Both services implement
`IAsyncDisposable`; dispose them after use. Receive methods run until their cancellation token is
cancelled and stop/dispose the processor on exit.

## Receiving Semantics

Subscribe to `MessageReceivedEvent` and `ErrorReceivedEvent` before starting a receiver. The common
handler raises the message event and then calls `CompleteMessageAsync`.
These are `EventHandler<T>` events, **not awaited asynchronous callbacks**. Do not attach an
`async void` subscriber and assume completion waits for its work; use the Azure SDK directly when
you need explicit asynchronous processing, settlement or retry control.

Information logging includes message bodies. Configure logging so sensitive payloads cannot reach
unapproved sinks. Error events include Azure SDK processing details.

Batch methods split messages into SDK-sized batches, but they **dequeue entries from the supplied
`Queue<ServiceBusMessage>` before sending**. A send failure can therefore leave the caller's queue
without the unsent entries. Preserve your own retry source, and reject individually oversized messages.

## Configuration

There is no options model or DI registration helper. Supply constructor arguments from the
consuming application's configuration and keep connection strings in a secret-backed provider.

## Documentation

- [Queue implementation](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.ServiceBus/Services/QueueService.cs)
- [Topic implementation](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.ServiceBus/Services/TopicService.cs)
- [Common event and completion behavior](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.ServiceBus/Services/Base/ServiceBase.cs)
- [Package catalogue and license](https://github.com/f2calv/CasCap.Api.Azure#packages)
