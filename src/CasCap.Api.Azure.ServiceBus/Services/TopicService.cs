namespace CasCap.Services;

/// <inheritdoc/>
public sealed class TopicService : ServiceBase, ITopicService
{
    private readonly string _topicName;
    private readonly string _subscriptionName;

    private readonly ServiceBusClient _client;

    /// <summary>Initializes a new instance of <see cref="TopicService"/> using a connection string.</summary>
    public TopicService(ILogger<TopicService> logger, string connectionString, string topicName, string subscriptionName) : base(logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        _topicName = topicName;
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
        _subscriptionName = subscriptionName;
        _client = new ServiceBusClient(connectionString);
    }

    /// <summary>Initializes a new instance of <see cref="TopicService"/> using a <see cref="TokenCredential"/>.</summary>
    public TopicService(ILogger<TopicService> logger, string fullyQualifiedNamespace, string topicName, string subscriptionName, TokenCredential credential) : base(logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullyQualifiedNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        _topicName = topicName;
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
        _subscriptionName = subscriptionName;
        ArgumentNullException.ThrowIfNull(credential);
        _client = new ServiceBusClient(fullyQualifiedNamespace, credential);
    }

    /// <inheritdoc/>
    public async Task SendMessageToTopicAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        // create a sender for the topic
        var sender = _client.CreateSender(_topicName);
        await sender.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("{ClassName} Sent a single message to the topic: {TopicName}",
            nameof(TopicService), _topicName);
    }

    /// <inheritdoc/>
    public async Task SendMessageBatchToTopicAsync(Queue<ServiceBusMessage> messages, CancellationToken cancellationToken = default)
    {
        // create a sender for the topic
        var sender = _client.CreateSender(_topicName);

        // total number of messages to be sent to the Service Bus topic
        var messageCount = messages.Count;

        // while all messages are not sent to the Service Bus topic
        while (messages.Count > 0)
        {
            // start a new batch
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

        _logger.LogInformation("{ClassName} Sent a batch of {MessageCount} messages to the topic: {TopicName}",
            nameof(TopicService), messageCount, _topicName);
    }

    /// <inheritdoc/>
    public async Task ReceiveMessagesFromSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        // create a processor that we can use to process the messages
        var processor = _client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions());

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
            _logger.LogInformation("{ClassName} Stopping the receiver...", nameof(TopicService));
            await processor.StopProcessingAsync(CancellationToken.None).ConfigureAwait(false);
            processor.ProcessMessageAsync -= MessageHandler;
            processor.ProcessErrorAsync -= ErrorHandler;
            await processor.DisposeAsync().ConfigureAwait(false);
            _logger.LogInformation("{ClassName} Stopped receiving messages", nameof(TopicService));
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
