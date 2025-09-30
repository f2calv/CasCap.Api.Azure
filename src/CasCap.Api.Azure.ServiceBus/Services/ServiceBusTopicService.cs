namespace CasCap.Services;

public class ServiceBusTopicService : ServiceBusServiceBase, IServiceBusQueueService
{
    private readonly string _connectionString;
    private readonly string _topicName;
    private readonly string _subscriptionName;

    public ServiceBusTopicService(ILogger<ServiceBusTopicService> logger, string connectionString, string topicName, string subscriptionName) : base(logger)
    {
        _connectionString = connectionString ?? throw new ArgumentException("not supplied!", nameof(connectionString));
        _topicName = topicName ?? throw new ArgumentException("not supplied!", nameof(topicName));
        _subscriptionName = subscriptionName ?? throw new ArgumentException("not supplied!", nameof(subscriptionName));
    }

    public async Task SendMessageToTopicAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        await using var client = new ServiceBusClient(_connectionString);
        // create a sender for the topic
        var sender = client.CreateSender(_topicName);
        await sender.SendMessageAsync(message, cancellationToken);
        _logger.LogInformation("{className} Sent a single message to the topic: {topicName}",
            nameof(ServiceBusTopicService), _topicName);
    }

    public async Task SendMessageBatchToTopicAsync(Queue<ServiceBusMessage> messages, CancellationToken cancellationToken = default)
    {
        await using var client = new ServiceBusClient(_connectionString);
        // create a sender for the topic
        var sender = client.CreateSender(_topicName);

        // total number of messages to be sent to the Service Bus topic
        var messageCount = messages.Count;

        // while all messages are not sent to the Service Bus topic
        while (messages.Count > 0)
        {
            // start a new batch
            using var messageBatch = await sender.CreateMessageBatchAsync(cancellationToken);
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
            await sender.SendMessagesAsync(messageBatch, cancellationToken);

            // if there are any remaining messages in the .NET queue, the while loop repeats
        }

        _logger.LogInformation("{className} Sent a batch of {messageCount} messages to the topic: {topicName}",
            nameof(ServiceBusTopicService), messageCount, _topicName);
    }

    public async Task ReceiveMessagesFromSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        await using var client = new ServiceBusClient(_connectionString);
        // create a processor that we can use to process the messages
        var processor = client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions());

        // add handler to process messages
        processor.ProcessMessageAsync += MessageHandler;

        // add handler to process any errors
        processor.ProcessErrorAsync += ErrorHandler;

        // start processing
        await processor.StartProcessingAsync(cancellationToken);

        // stop processing
        _logger.LogInformation("{className} Stopping the receiver...", nameof(ServiceBusTopicService));
        await processor.StopProcessingAsync(cancellationToken);
        _logger.LogInformation("{className} Stopped receiving messages", nameof(ServiceBusTopicService));
    }
}
