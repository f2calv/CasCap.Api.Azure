using System.Text;
namespace CasCap.Services;

public interface IEventHubPublisherService<T>// where T : IEventHubEvent
{
    Task Push(T obj);
    Task Push(byte[] bytes);
    Task Push(List<T> objs);
    Task Push(List<byte[]> bytesCollection);
    Task SendTestMessages(int numMessagesToSend = 10);
}

public abstract class EventHubPublisherService<T> : IEventHubPublisherService<T>// where T : IEventHubEvent
{
    readonly ILogger _logger;

    readonly string _connectionString;

    public EventHubPublisherService(ILogger<EventHubPublisherService<T>> logger, string connectionString, string EntityPath)
    {
        _logger = logger;
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

    public async Task Push(T obj) => await Push(new List<byte[]>(1) { obj.ToMessagePack() });

    public async Task Push(byte[] bytes) => await Push(new List<byte[]>(1) { bytes });

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
            _logger.LogError(ex, $"{nameof(Push)} failure");
            throw;
        }
    }

    public async Task SendTestMessages(int numMessagesToSend = 10)
    {
        for (var i = 0; i < numMessagesToSend; i++)
        {
            var message = $"Message {i}";
            //_logger.LogDebug("Sending message: {message}", message);
            await Push(Encoding.UTF8.GetBytes(message));
        }
        _logger.LogDebug("{numMessagesToSend} messages sent.", numMessagesToSend);
    }
}
