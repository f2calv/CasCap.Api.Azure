namespace CasCap.Abstractions;

/// <summary>Abstraction for Azure Service Bus topic send and subscription receive operations.</summary>
/// <remarks>
/// Implementations own a single <see cref="Azure.Messaging.ServiceBus.ServiceBusClient"/> for the
/// lifetime of the instance; dispose the service (via <see cref="IAsyncDisposable"/>) to release it.
/// </remarks>
public interface ITopicService : IAsyncDisposable
{
    /// <summary>Sends a single <paramref name="message"/> to the topic.</summary>
    /// <param name="message">The Service Bus message to send.</param>
    /// <param name="cancellationToken">Token to cancel the send operation.</param>
    Task SendMessageToTopicAsync(ServiceBusMessage message, CancellationToken cancellationToken = default);

    /// <summary>Sends a batch of <paramref name="messages"/> to the topic, splitting into multiple batches as required.</summary>
    /// <param name="messages">The messages to send; dequeued as they are added to a batch.</param>
    /// <param name="cancellationToken">Token to cancel the send operation.</param>
    Task SendMessageBatchToTopicAsync(Queue<ServiceBusMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts receiving messages from the topic subscription and processes them until
    /// <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    /// <remarks>
    /// Received messages raise <see cref="CasCap.Services.ServiceBase.MessageReceivedEvent"/>;
    /// errors raise <see cref="CasCap.Services.ServiceBase.ErrorReceivedEvent"/>.
    /// </remarks>
    /// <param name="cancellationToken">Token used to stop receiving.</param>
    Task ReceiveMessagesFromSubscriptionAsync(CancellationToken cancellationToken = default);
}
