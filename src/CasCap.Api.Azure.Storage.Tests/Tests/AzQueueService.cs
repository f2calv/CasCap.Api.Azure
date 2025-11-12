namespace CasCap.Tests;

public interface IAzQueueService : IAzQueueStorageBase
{
}

public class AzQueueService(string connectionString, string queueName = "wibble")
    : AzQueueStorageBase(connectionString, queueName), IAzQueueService
{
}
