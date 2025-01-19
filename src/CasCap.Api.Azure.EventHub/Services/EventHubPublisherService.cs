namespace CasCap.Services;

public abstract class EventHubPublisherService<T> : IEventHubPublisherService<T>// where T : IEventHubEvent
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(EventHubPublisherService<T>));

    private readonly string _connectionString;

    public EventHubPublisherService(string connectionString, string EntityPath)
    {
        _connectionString = connectionString ?? throw new ArgumentException("required!", nameof(connectionString));

        _producerClient = new EventHubProducerClient(_connectionString, new EventHubProducerClientOptions
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

    EventHubProducerClient _producerClient { get; }

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
                    throw new Exception($"EventHubName={typeof(T).Name}, batch size too big!");
            }
            await _producerClient.SendAsync(eventBatch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{className} {methodName} failure", nameof(EventHubPublisherService<T>), nameof(Push));
            throw;
        }
    }

    public async Task SendTestMessages(int numMessagesToSend = 10)
    {
        for (var i = 0; i < numMessagesToSend; i++)
        {
            var message = $"Message {i}";
            //_logger.LogDebug("{className} Sending message: {message}", nameof(EventHubPublisherService<T>), message);
            await Push(Encoding.UTF8.GetBytes(message));
        }
        _logger.LogDebug("{className} {numMessagesToSend} messages sent.", nameof(EventHubPublisherService<T>), numMessagesToSend);
    }
}
