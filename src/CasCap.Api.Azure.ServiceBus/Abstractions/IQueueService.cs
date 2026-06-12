namespace CasCap.Abstractions;

/// <summary>Abstraction for Azure Service Bus queue send and receive operations.</summary>
/// <remarks>
/// Implementations own a single <see cref="Azure.Messaging.ServiceBus.ServiceBusClient"/> for the
/// lifetime of the instance; dispose the service (via <see cref="IAsyncDisposable"/>) to release it.
/// </remarks>
public interface IQueueService : IAsyncDisposable
{
    /// <summary>Sends a single <paramref name="message"/> to the queue.</summary>
    /// <param name="message">The Service Bus message to send.</param>
    /// <param name="cancellationToken">Token to cancel the send operation.</param>
    Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default);

    /// <summary>Sends a batch of <paramref name="messages"/> to the queue, splitting into multiple batches as required.</summary>
    /// <param name="messages">The messages to send; dequeued as they are added to a batch.</param>
    /// <param name="cancellationToken">Token to cancel the send operation.</param>
    Task SendMessageBatchAsync(Queue<ServiceBusMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts receiving messages from the queue and processes them until <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    /// <remarks>
    /// Received messages raise <see cref="CasCap.Services.ServiceBase.MessageReceivedEvent"/>;
    /// errors raise <see cref="CasCap.Services.ServiceBase.ErrorReceivedEvent"/>.
    /// </remarks>
    /// <param name="cancellationToken">Token used to stop receiving.</param>
    Task ReceiveMessagesAsync(CancellationToken cancellationToken = default);
}
