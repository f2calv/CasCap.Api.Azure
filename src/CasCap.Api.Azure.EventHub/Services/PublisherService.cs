namespace CasCap.Services;

/// <inheritdoc/>
public abstract class PublisherService<T> : IPublisherService<T>// where T : IEvent
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(PublisherService<T>));

    private readonly EventHubProducerClient _producerClient;

    /// <summary>Initializes a new instance of <see cref="PublisherService{T}"/> using a connection string.</summary>
    protected PublisherService(string connectionString, string entityPath)
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

    /// <summary>Initializes a new instance of <see cref="PublisherService{T}"/> using a <see cref="TokenCredential"/>.</summary>
    protected PublisherService(string fullyQualifiedNamespace, string eventHubName, TokenCredential credential,
        EventHubProducerClientOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullyQualifiedNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventHubName);
        _producerClient = new EventHubProducerClient(fullyQualifiedNamespace, eventHubName, credential, options);
    }

    /// <inheritdoc/>
    public Task Push(T obj) => Push([obj.ToMessagePack()]);

    /// <inheritdoc/>
    public Task Push(byte[] bytes) => Push([bytes]);

    /// <inheritdoc/>
    public async Task Push(List<T> objs)
    {
        var l = new List<byte[]>(objs.Count);
        foreach (var obj in objs)
            l.Add(obj.ToMessagePack());
        await Push(l);
    }

    /// <inheritdoc/>
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
            _logger.LogError(ex, "{ClassName} {MethodName} failure", nameof(PublisherService<T>), nameof(Push));
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SendTestMessages(int numMessagesToSend = 10)
    {
        for (var i = 0; i < numMessagesToSend; i++)
        {
            var message = $"Message {i}";
            //_logger.LogDebug("{ClassName} Sending message: {Message}", nameof(PublisherService<T>), message);
            await Push(Encoding.UTF8.GetBytes(message));
        }
        _logger.LogDebug("{ClassName} {NumMessagesToSend} messages sent.", nameof(PublisherService<T>), numMessagesToSend);
    }
}
