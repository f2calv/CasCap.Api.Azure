using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
namespace CasCap.Services;

public interface IServiceBusQueueService
{
}

public class ServiceBusQueueService : ServiceBusServiceBase, IServiceBusQueueService
{
    readonly string _connectionString;
    readonly string _queueName;

    public ServiceBusQueueService(ILogger<ServiceBusQueueService> logger, string connectionString, string queueName) : base(logger)
    {
        _connectionString = connectionString ?? throw new ArgumentException("not supplied!", nameof(connectionString));
        _queueName = queueName ?? throw new ArgumentException("not supplied!", nameof(queueName));
    }

    public async Task SendMessageAsync(ServiceBusMessage message)
    {
        await using (var client = new ServiceBusClient(_connectionString))
        {
            // create a sender for the queue
            var sender = client.CreateSender(_queueName);

            // send the message
            await sender.SendMessageAsync(message);
            _logger.LogInformation("Sent a single message to the queue: {queueName}", _queueName);
        }
    }

    public async Task SendMessageBatchAsync(Queue<ServiceBusMessage> messages, CancellationToken cancellationToken = default)
    {
        await using (var client = new ServiceBusClient(_connectionString))
        {
            // create a sender for the queue
            var sender = client.CreateSender(_queueName);

            // total number of messages to be sent to the Service Bus queue
            var messageCount = messages.Count;

            // while all messages are not sent to the Service Bus queue
            while (messages.Count > 0)
            {
                using (var messageBatch = await sender.CreateMessageBatchAsync(cancellationToken))
                {
                    // add the first message to the batch
                    if (messageBatch.TryAddMessage(messages.Peek()))
                    {
                        // dequeue the message from the .NET queue once the message is added to the batch
                        messages.Dequeue();
                    }
                    else
                    {
                        // if the first message can't fit, then it is too large for the batch
                        throw new Exception($"Message {messageCount - messages.Count} is too large and cannot be sent.");
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
            }

            _logger.LogInformation("Sent a batch of {messageCount} messages to the topic: {queueName}", messageCount, _queueName);
        }
    }

    public async Task ReceiveMessagesAsync(CancellationToken cancellationToken = default)
    {
        await using (var client = new ServiceBusClient(_connectionString))
        {
            // create a processor that we can use to process the messages
            var processor = client.CreateProcessor(_queueName, new ServiceBusProcessorOptions());

            // add handler to process messages
            processor.ProcessMessageAsync += MessageHandler;

            // add handler to process any errors
            processor.ProcessErrorAsync += ErrorHandler;

            // start processing
            await processor.StartProcessingAsync(cancellationToken);

            // stop processing
            _logger.LogInformation("Stopping the receiver...");
            await processor.StopProcessingAsync(cancellationToken);
            _logger.LogInformation("Stopped receiving messages");
        }
    }
}
