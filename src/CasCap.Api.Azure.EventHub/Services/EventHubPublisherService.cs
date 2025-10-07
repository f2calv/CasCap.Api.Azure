namespace CasCap.Services;

public abstract class EventHubPublisherService<T> : IEventHubPublisherService<T>// where T : IEventHubEvent
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(EventHubPublisherService<T>));

    private readonly EventHubProducerClient _producerClient;

    protected EventHubPublisherService(string connectionString, string entityPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _producerClient = new EventHubProducerClient(connectionString, new EventHubProducerClientOptions
        {
            ConnectionOptions = new EventHubConnectionOptions
            {
                //TransportType = TransportType.Amqp,
                //TransportType = TransportType.AmqpWebSockets,
                //OperationTimeout = TimeSpan.FromSeconds(60),//?
            },
            //Identifier = Environment.MachineName,
            //RetryOptions = new EventHubsRetryOptions { }
        });
    }

    protected EventHubPublisherService(string fullyQualifiedNamespace, string eventHubName, TokenCredential credential,
        EventHubProducerClientOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullyQualifiedNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventHubName);
        _producerClient = new EventHubProducerClient(fullyQualifiedNamespace, eventHubName, credential, options);
    }

    public async Task Push(T obj) => await Push([obj.ToMessagePack()]);

    public async Task Push(byte[] bytes) => await Push([bytes]);

    public async Task Push(List<T> objs)
    {
        var l = new List<byte[]>(objs.Count);
        foreach (var obj in objs)
            l.Add(obj.ToMessagePack());
        await Push(l);
    }

    public async Task Push(List<byte[]> bytesCollection)
    {
        using var eventBatch = await _producerClient.CreateBatchAsync();
        try
        {
            foreach (var bytes in bytesCollection)
            {
                var data = new EventData(bytes);
                if (!eventBatch.TryAdd(data))
                    throw new GenericException($"EventHubName={typeof(T).Name}, batch size too big!");
            }
            await _producerClient.SendAsync(eventBatch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ClassName} {MethodName} failure", nameof(EventHubPublisherService<T>), nameof(Push));
            throw;
        }
    }

    public async Task SendTestMessages(int numMessagesToSend = 10)
    {
        for (var i = 0; i < numMessagesToSend; i++)
        {
            var message = $"Message {i}";
            //_logger.LogDebug("{ClassName} Sending message: {Message}", nameof(EventHubPublisherService<T>), message);
            await Push(Encoding.UTF8.GetBytes(message));
        }
        _logger.LogDebug("{ClassName} {NumMessagesToSend} messages sent.", nameof(EventHubPublisherService<T>), numMessagesToSend);
    }
}
