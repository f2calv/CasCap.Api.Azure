namespace CasCap.Services;

//https://docs.microsoft.com/en-us/azure/storage/queues/storage-tutorial-queues
//https://docs.microsoft.com/en-us/azure/storage/queues/storage-quickstart-queues-dotnet
/// <inheritdoc/>
public abstract class AzQueueStorageBase : IAzQueueStorageBase
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(AzQueueStorageBase));

    private readonly QueueClient _queueClient;

    /// <summary>Initializes a new instance of <see cref="AzQueueStorageBase"/> using a connection string.</summary>
    protected AzQueueStorageBase(string connectionString, string queueName, QueueClientOptions.ServiceVersion? serviceVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        //https://github.com/Azure/azure-sdk-for-net/issues/10242
        var options = serviceVersion.HasValue
            ? new QueueClientOptions(serviceVersion.Value) { MessageEncoding = QueueMessageEncoding.Base64 }
            : new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 };
        _queueClient = new QueueClient(connectionString, queueName, options);
    }

    /// <summary>Initializes a new instance of <see cref="AzQueueStorageBase"/> using a <see cref="TokenCredential"/>.</summary>
    protected AzQueueStorageBase(Uri queueUri, string queueName, TokenCredential credential, QueueClientOptions.ServiceVersion? serviceVersion = null)
    {
        ArgumentNullException.ThrowIfNull(queueUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(credential);
        //https://github.com/Azure/azure-sdk-for-net/issues/10242
        var options = serviceVersion.HasValue
            ? new QueueClientOptions(serviceVersion.Value) { MessageEncoding = QueueMessageEncoding.Base64 }
            : new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 };
        _queueClient = new QueueClient(new Uri(queueUri, queueName), credential, options);
    }

    private bool _haveCheckedIfQueueExists = false;

    private async ValueTask CreateQueueIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        if (!_haveCheckedIfQueueExists && (await _queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken) is not null))
            _logger.LogDebug("{ClassName} storage queue didn't exist so have now created {QueueName}", nameof(AzQueueStorageBase), _queueClient.Name);
        _haveCheckedIfQueueExists = true;
    }

    /// <inheritdoc/>
    public Task<bool> Enqueue<T>(T obj, CancellationToken cancellationToken = default) where T : class => Enqueue([obj], cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> Enqueue<T>(List<T> objs, CancellationToken cancellationToken = default) where T : class
    {
        await CreateQueueIfNotExistsAsync(cancellationToken);
        var i = 1;
        foreach (var obj in objs)
        {
            if (obj is null)
            {
                _logger.LogWarning("{ClassName} obj is null?", nameof(AzQueueStorageBase));
                continue;
            }
            var json = obj.ToJson();
            var message = new BinaryData(json);
            Azure.Response<SendReceipt>? result = null;
            try
            {
                result = await _queueClient.SendMessageAsync(message, default, default, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} failed to insert {MessageType} into storage queue '{QueueName}' JSON content is '{Bytes}' bytes",
                    nameof(AzQueueStorageBase), typeof(T).Name, _queueClient.Name, message.ToArray().Length);
            }
            if (result is not null && result.Value is not null)
            {
                _logger.LogDebug("{ClassName} {MessageType} {Iteration} of {MessageCount} inserted into storage queue '{QueueName}', MessageId={MessageId}",
                    nameof(AzQueueStorageBase), typeof(T).Name, i, objs.Count, _queueClient.Name, result.Value.MessageId);
                i++;
            }
        }
        return i > 0;
    }

    /// <inheritdoc/>
    public async Task<(T?, QueueMessage)> DequeueSingle<T>(CancellationToken cancellationToken = default) where T : class
    {
        //_logger.LogTrace("{ClassName} trying account {AccountName}...", nameof(AzQueueStorageBase), _queueClient.AccountName);
        await CreateQueueIfNotExistsAsync(cancellationToken);
        // Get the next message
        var retrievedMessage = await _queueClient.ReceiveMessageAsync(cancellationToken: cancellationToken);
        if (retrievedMessage is not null && retrievedMessage.Value is not null)
        {
            var json = retrievedMessage.Value.Body.ToString();
            T? obj = null;
            var IsCorrupted = false;
            try
            {
                obj = json.FromJson<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} failed to deserialize JSON, '{Json}'", nameof(AzQueueStorageBase), json);
                IsCorrupted = true;
            }
            finally
            {
                _ = await _queueClient.DeleteMessageAsync(retrievedMessage.Value.MessageId, retrievedMessage.Value.PopReceipt, cancellationToken);
                if (IsCorrupted)
                    _logger.LogWarning("{ClassName} removed message id {Id} from queue as it failed deserialization, '{Json}'",
                        nameof(AzQueueStorageBase), retrievedMessage.Value.MessageId, json);
                else
                    _logger.LogInformation("{ClassName} removed message id {Id} from queue as it passed deserialization!",
                        nameof(AzQueueStorageBase), retrievedMessage.Value.MessageId);
            }
            return (obj, retrievedMessage.Value);
        }
        else
            return (default, default!);
    }

    /// <inheritdoc/>
    public async Task<List<T>> DequeueMany<T>(int limit = 1, CancellationToken cancellationToken = default) where T : class
    {
        await CreateQueueIfNotExistsAsync(cancellationToken);
        var messages = await Dequeue(limit, cancellationToken);
        var l = new List<T>(messages.Count);
        foreach (var retrievedMessage in messages)
        {
            var json = retrievedMessage.Body.ToString();
            var obj = json.FromJson<T>();
            l.Add(obj!);
            //delete each message after processing
            await _queueClient.DeleteMessageAsync(retrievedMessage.MessageId, retrievedMessage.PopReceipt, cancellationToken);
        }
        return l;
    }

    private async Task<List<QueueMessage>> Dequeue(int limit = 1, CancellationToken cancellationToken = default)
    {
        var properties = await _queueClient.GetPropertiesAsync(cancellationToken);
        if (properties.Value.ApproximateMessagesCount > 1) limit = properties.Value.ApproximateMessagesCount;
        var l = new List<QueueMessage>(limit);
        var messages = await _queueClient.ReceiveMessagesAsync(limit, cancellationToken: cancellationToken);
        foreach (var retrievedMessage in messages.Value)
            l.Add(retrievedMessage);
        return l;
    }
}
