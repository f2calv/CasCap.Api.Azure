namespace CasCap.Services;

/// <inheritdoc/>
/// <remarks>See <see href="https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Azure.Messaging.EventHubs/MigrationGuide.md" />.</remarks>
public abstract class SubscriberService<T> : ISubscriberService<T>
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(SubscriberService<>));

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
            await _checkpointStore.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            _eventProcessorClient.ProcessEventAsync += ProcessEventHandler;
            _eventProcessorClient.ProcessErrorAsync += ProcessErrorHandler;
            try
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("{ClassName} _eventProcessorClient.StartProcessingAsync... for {EventHubName}", nameof(SubscriberService<>), _eventProcessorClient.EventHubName);
                await _eventProcessorClient.StartProcessingAsync(cancellationToken).ConfigureAwait(false);
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // This is expected if the cancellation token is signaled.
            }
            finally
            {
                // This may take up to the length of time defined as part of the configured TryTimeout of the processor; by default, this is 60 seconds.
                await _eventProcessorClient.StopProcessingAsync(cancellationToken).ConfigureAwait(false);
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
            if (bytes is not null)
            {
                _ = bytes.FromMessagePack<T>();
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("{ClassName} message received PartitionId={PartitionId} ByteCount={ByteCount}",
                        nameof(SubscriberService<>), partitionId, bytes.Length);
                }
            }
            else
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.LogWarning("{ClassName} message received without data PartitionId={PartitionId}",
                        nameof(SubscriberService<>), partitionId);
            }

            var eventsSinceLastCheckpoint = partitionEventCount.AddOrUpdate(
                key: partitionId,
                addValue: 1,
                updateValueFactory: (_, currentCount) => currentCount + 1);

            if (eventsSinceLastCheckpoint >= 50)
            {
                await args.UpdateCheckpointAsync().ConfigureAwait(false);
                partitionEventCount[partitionId] = 0;
            }
        }
        catch (Exception ex)
        {
            // It is very important that you always guard against exceptions in your handler code; the processor does not have enough understanding of your code to determine the correct action to take.
            // Any exceptions from your handlers go uncaught by the processor and will NOT be redirected to the error handler.
            if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError(ex, "{ClassName} failed to process event PartitionId={PartitionId}",
                    nameof(SubscriberService<>), args.Partition.PartitionId);
        }
    }

    private static Task ProcessErrorHandler(ProcessErrorEventArgs args)
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError(args.Exception, "{ClassName} error detected in operation {Operation}",
                    nameof(SubscriberService<>), args.Operation);
        }
        catch
        {
            // It is very important that you always guard against exceptions in your handler code; the processor does not have enough understanding of your code to determine the correct action to take.
            // Any exceptions from your handlers go uncaught by the processor and will NOT be handled in any way.
        }

        return Task.CompletedTask;
    }
}
