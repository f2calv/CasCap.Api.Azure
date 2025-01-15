namespace CasCap.Tests;

public interface IAzQueueService : IAzQueueStorageBase
{
}

public class AzQueueService(string connectionString)
    : AzQueueStorageBase(connectionString, queueName: "wibble"), IAzQueueService
{
}
