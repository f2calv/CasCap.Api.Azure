namespace CasCap.Services;

public interface IAzQueueStorageBase
{
    Task<(T? obj, QueueMessage message)> DequeueSingle<T>() where T : class;
    Task<List<T>> DequeueMany<T>(int limit = 1) where T : class;
    Task<bool> Enqueue<T>(T obj) where T : class;
    Task<bool> Enqueue<T>(List<T> objs) where T : class;
}

//https://docs.microsoft.com/en-us/azure/storage/queues/storage-tutorial-queues
//https://docs.microsoft.com/en-us/azure/storage/queues/storage-quickstart-queues-dotnet
public abstract class AzQueueStorageBase : IAzQueueStorageBase
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(AzQueueStorageBase));
    private readonly string _connectionString;
    private readonly string _queueName;

    private readonly QueueClient _queueClient;

    public AzQueueStorageBase(string connectionString, string queueName)
    {
        _connectionString = connectionString ?? throw new ArgumentException("not supplied!", nameof(connectionString));
        _queueName = queueName ?? throw new ArgumentException("not supplied!", nameof(queueName));

        if (string.IsNullOrWhiteSpace(_connectionString) || string.IsNullOrWhiteSpace(_queueName))
            throw new ArgumentException("connectionString and/or _queueName not set!");

        _queueClient = new QueueClient(_connectionString, _queueName,
            //https://github.com/Azure/azure-sdk-for-net/issues/10242
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
    }

    private bool _haveCheckedIfQueueExists = false;

    private async ValueTask CreateQueueIfNotExistsAsync()
    {
        if (!_haveCheckedIfQueueExists && (await _queueClient.CreateIfNotExistsAsync() != null))
            _logger.LogDebug("{className} storage queue didn't exist so have now created '{queueName}'", nameof(AzQueueStorageBase), _queueName);
        _haveCheckedIfQueueExists = true;
    }

    public async Task<bool> Enqueue<T>(T obj) where T : class => await Enqueue([obj]);

    public async Task<bool> Enqueue<T>(List<T> objs) where T : class
    {
        await CreateQueueIfNotExistsAsync();
        var i = 1;
        foreach (var obj in objs)
        {
            if (obj is null)
            {
                _logger.LogWarning("{className} obj is null?", nameof(AzQueueStorageBase));
                continue;
            }
            var json = obj.ToJson();
            var message = new BinaryData(json);
            Azure.Response<SendReceipt>? result = null;
            try
            {
                result = await _queueClient.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{className} failed to insert {messageType} into storage queue '{queueName}' JSON content is '{bytes}' bytes",
                    nameof(AzQueueStorageBase), typeof(T).Name, _queueName, message.ToArray().Length);
            }
            if (result is not null && result.Value is not null)
            {
                _logger.LogDebug("{messageType} {i} of {messageCount} inserted into storage queue '{queueName}', MessageId={MessageId}",
                    typeof(T).Name, i, objs.Count, _queueName, result.Value.MessageId);
                i++;
            }
        }
        return i > 0;
    }

    public async Task<(T?, QueueMessage)> DequeueSingle<T>() where T : class
    {
        //_logger.LogTrace("Trying account {accountName}...", _queueClient.AccountName);
        await CreateQueueIfNotExistsAsync();
        // Get the next message
        var retrievedMessage = await _queueClient.ReceiveMessageAsync();
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
                _logger.LogError(ex, "{className} failed to deserialize JSON, '{json}'", nameof(AzQueueStorageBase), json);
                IsCorrupted = true;
            }
            finally
            {
                _ = await _queueClient.DeleteMessageAsync(retrievedMessage.Value.MessageId, retrievedMessage.Value.PopReceipt);
                if (IsCorrupted)
                    _logger.LogWarning("{className} removed message id {id} from queue as it failed deserialization, '{json}'",
                        nameof(AzQueueStorageBase), retrievedMessage.Value.MessageId, json);
                else
                    _logger.LogInformation("{className} removed message id {id} from queue as it passed deserialization!",
                        nameof(AzQueueStorageBase), retrievedMessage.Value.MessageId);
            }
            return (obj, retrievedMessage.Value);
        }
        else
            return (default, default!);
    }

    public async Task<List<T>> DequeueMany<T>(int limit = 1) where T : class
    {
        await CreateQueueIfNotExistsAsync();
        var messages = await Dequeue(limit);
        var l = new List<T>(messages.Count);
        foreach (var retrievedMessage in messages)
        {
            var json = retrievedMessage.Body.ToString();
            var obj = json.FromJson<T>();
            l.Add(obj!);
            //delete each message after processing
            await _queueClient.DeleteMessageAsync(retrievedMessage.MessageId, retrievedMessage.PopReceipt);
        }
        return l;
    }

    private async Task<List<QueueMessage>> Dequeue(int limit = 1)
    {
        var properties = await _queueClient.GetPropertiesAsync();
        if (properties.Value.ApproximateMessagesCount > 1) limit = properties.Value.ApproximateMessagesCount;
        var l = new List<QueueMessage>(limit);
        var messages = await _queueClient.ReceiveMessagesAsync(limit);
        foreach (var retrievedMessage in messages.Value)
            l.Add(retrievedMessage);
        return l;
    }
}
