namespace CasCap.Services;

/// <inheritdoc/>
/// <remarks>See <see href="https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Azure.Messaging.EventHubs/MigrationGuide.md" />.</remarks>
public abstract class SubscriberService<T> : ISubscriberService<T>
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(SubscriberService<T>));

    private readonly BlobContainerClient _checkpointStore;
    private readonly EventProcessorClient _eventProcessorClient;

    /// <summary>Initializes a new instance of <see cref="SubscriberService{T}"/> using connection strings.</summary>
    protected SubscriberService(
        string eventHubName,
        string eventHubConnectionString,
        string storageConnectionString,
        string leaseContainerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventHubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventHubConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseContainerName);

        _checkpointStore = new BlobContainerClient(storageConnectionString, blobContainerName: leaseContainerName);

        _eventProcessorClient = new EventProcessorClient(
            _checkpointStore,
            EventHubConsumerClient.DefaultConsumerGroupName,
            eventHubConnectionString,
            eventHubName);
    }

    /// <summary>Initializes a new instance of <see cref="SubscriberService{T}"/> using a <see cref="TokenCredential"/>.</summary>
    protected SubscriberService(
        string eventHubName,
        string eventHubConnectionString,
        string storageConnectionString,
        string leaseContainerName,
        string fullyQualifiedNamespace,
        TokenCredential credential)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventHubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventHubConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseContainerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullyQualifiedNamespace);
        ArgumentNullException.ThrowIfNull(credential);

        _checkpointStore = new BlobContainerClient(storageConnectionString, blobContainerName: leaseContainerName);

        _eventProcessorClient = new EventProcessorClient(
            _checkpointStore,
            EventHubConsumerClient.DefaultConsumerGroupName,
            fullyQualifiedNamespace,
            eventHubName,
            credential);
    }

    private readonly ConcurrentDictionary<string, int> partitionEventCount = new();

    /// <inheritdoc/>
    public async Task InitiateReceive(CancellationToken cancellationToken)
    {
        try
        {
            await _checkpointStore.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            _eventProcessorClient.ProcessEventAsync += ProcessEventHandler;
            _eventProcessorClient.ProcessErrorAsync += ProcessErrorHandler;
            try
            {
                _logger.LogDebug("{ClassName} _eventProcessorClient.StartProcessingAsync... for {EventHubName}", nameof(SubscriberService<T>), _eventProcessorClient.EventHubName);
                await _eventProcessorClient.StartProcessingAsync(cancellationToken);
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // This is expected if the cancellation token is signaled.
            }
            finally
            {
                // This may take up to the length of time defined as part of the configured TryTimeout of the processor; by default, this is 60 seconds.
                await _eventProcessorClient.StopProcessingAsync(cancellationToken);
            }
        }
        catch
        {
            // The processor will automatically attempt to recover from any failures, either transient or fatal, and continue processing.
            // Errors in the processor's operation will be surfaced through its error handler.
            //
            // If this block is invoked, then something external to the processor was the source of the exception.
        }
        finally
        {
            // It is encouraged that you unregister your handlers when you have finished using the Event Processor to ensure proper cleanup.
            // This is especially important when using lambda expressions or handlers in any form that may contain closure scopes or hold other references.
            _eventProcessorClient.ProcessEventAsync -= ProcessEventHandler;
            _eventProcessorClient.ProcessErrorAsync -= ProcessErrorHandler;
        }
    }

    private async Task ProcessEventHandler(ProcessEventArgs args)
    {
        try
        {
            // If the cancellation token is signaled, then the processor has been asked to stop. It will invoke this handler with any events that were in flight;
            // these will not be lost if not processed.
            //
            // It is up to the handler to decide whether to take action to process the event or to cancel immediately.

            if (args.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var partitionId = args.Partition.PartitionId;
            var bytes = args.Data.EventBody.ToArray();
            //_logger.LogDebug($"Event from partition { partitionId } with length { bytes.Length }.");
            if (bytes is not null)
            {
                var obj = bytes.FromMessagePack<T>();
                _logger.LogInformation("{ClassName} Message received. Partition: {PartitionId}, Data: {Obj}",
                    nameof(SubscriberService<T>), partitionId, obj);
            }
            else
                _logger.LogWarning("{ClassName} Message received. Partition: {PartitionId}, Data: null",
                    nameof(SubscriberService<T>), partitionId);

            var eventsSinceLastCheckpoint = partitionEventCount.AddOrUpdate(
                key: partitionId,
                addValue: 1,
                updateValueFactory: (_, currentCount) => currentCount + 1);

            if (eventsSinceLastCheckpoint >= 50)
            {
                await args.UpdateCheckpointAsync();
                partitionEventCount[partitionId] = 0;
            }
        }
        catch
        {
            // It is very important that you always guard against exceptions in your handler code; the processor does not have enough understanding of your code to determine the correct action to take.
            // Any exceptions from your handlers go uncaught by the processor and will NOT be redirected to the error handler.
        }
    }

    private static Task ProcessErrorHandler(ProcessErrorEventArgs args)
    {
        try
        {
            _logger.LogError(args.Exception, "{ClassName} error detected in operation {Operation}",
                nameof(SubscriberService<T>), args.Operation);
        }
        catch
        {
            // It is very important that you always guard against exceptions in your handler code; the processor does not have enough understanding of your code to determine the correct action to take.
            // Any exceptions from your handlers go uncaught by the processor and will NOT be handled in any way.
        }

        return Task.CompletedTask;
    }
}
