namespace CasCap.Services;

/// <inheritdoc/>
public sealed class QueueService : ServiceBase, IQueueService
{
    private readonly string _queueName;

    private readonly ServiceBusClient _client;

    /// <summary>Initializes a new instance of <see cref="QueueService"/> using a connection string.</summary>
    public QueueService(ILogger<QueueService> logger, string connectionString, string queueName) : base(logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        _queueName = queueName;
        _client = new ServiceBusClient(connectionString);
    }

    /// <summary>Initializes a new instance of <see cref="QueueService"/> using a <see cref="TokenCredential"/>.</summary>
    public QueueService(ILogger<QueueService> logger, string fullyQualifiedNamespace, string queueName, TokenCredential credential) : base(logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullyQualifiedNamespace);
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        _queueName = queueName;
        _client = new ServiceBusClient(fullyQualifiedNamespace, credential);
    }

    /// <inheritdoc/>
    public async Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        // create a sender for the queue
        var sender = _client.CreateSender(_queueName);

        // send the message
        await sender.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("{ClassName} Sent a single message to the queue: {QueueName}",
            nameof(QueueService), _queueName);
    }

    /// <inheritdoc/>
    public async Task SendMessageBatchAsync(Queue<ServiceBusMessage> messages, CancellationToken cancellationToken = default)
    {
        // create a sender for the queue
        var sender = _client.CreateSender(_queueName);

        // total number of messages to be sent to the Service Bus queue
        var messageCount = messages.Count;

        // while all messages are not sent to the Service Bus queue
        while (messages.Count > 0)
        {
            using var messageBatch = await sender.CreateMessageBatchAsync(cancellationToken).ConfigureAwait(false);
            // add the first message to the batch
            if (messageBatch.TryAddMessage(messages.Peek()))
            {
                // dequeue the message from the .NET queue once the message is added to the batch
                messages.Dequeue();
            }
            else
            {
                // if the first message can't fit, then it is too large for the batch
                throw new GenericException($"Message {messageCount - messages.Count} is too large and cannot be sent.");
            }

            // add as many messages as possible to the current batch
            while (messages.Count > 0 && messageBatch.TryAddMessage(messages.Peek()))
            {
                // dequeue the message from the .NET queue as it has been added to the batch
                messages.Dequeue();
            }

            // now, send the batch
            await sender.SendMessagesAsync(messageBatch, cancellationToken).ConfigureAwait(false);

            // if there are any remaining messages in the .NET queue, the while loop repeats
        }

        _logger.LogInformation("{ClassName} Sent a batch of {MessageCount} messages to the queue: {QueueName}",
            nameof(QueueService), messageCount, _queueName);
    }

    /// <inheritdoc/>
    public async Task ReceiveMessagesAsync(CancellationToken cancellationToken = default)
    {
        // create a processor that we can use to process the messages
        var processor = _client.CreateProcessor(_queueName, new ServiceBusProcessorOptions());

        // add handler to process messages
        processor.ProcessMessageAsync += MessageHandler;

        // add handler to process any errors
        processor.ProcessErrorAsync += ErrorHandler;

        try
        {
            // start processing and keep running until cancellation is requested
            await processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // expected when the caller cancels to stop receiving
        }
        finally
        {
            _logger.LogInformation("{ClassName} Stopping the receiver...", nameof(QueueService));
            await processor.StopProcessingAsync(CancellationToken.None).ConfigureAwait(false);
            processor.ProcessMessageAsync -= MessageHandler;
            processor.ProcessErrorAsync -= ErrorHandler;
            await processor.DisposeAsync().ConfigureAwait(false);
            _logger.LogInformation("{ClassName} Stopped receiving messages", nameof(QueueService));
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
