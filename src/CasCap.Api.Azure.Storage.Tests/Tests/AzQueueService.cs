using Azure.Storage.Queues;

namespace CasCap.Tests;

/// <summary>Concrete queue storage service used in integration tests.</summary>
public class AzQueueService(string connectionString, string queueName = "wibble")
    : AzQueueStorageBase(connectionString, queueName, QueueClientOptions.ServiceVersion.V2025_11_05), IAzQueueService
{
}
