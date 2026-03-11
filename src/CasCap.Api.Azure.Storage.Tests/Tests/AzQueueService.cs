namespace CasCap.Tests;

/// <summary>Concrete queue storage service used in integration tests.</summary>
public class AzQueueService(string connectionString, string queueName = "wibble")
    : AzQueueStorageBase(connectionString, queueName), IAzQueueService
{
}
