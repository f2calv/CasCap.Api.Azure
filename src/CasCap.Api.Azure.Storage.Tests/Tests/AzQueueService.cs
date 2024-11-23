namespace CasCap.Tests;

public interface IAzQueueService : IAzQueueStorageBase
{
}

public class AzQueueService(ILogger<AzQueueService> logger, string connectionString)
    : AzQueueStorageBase(logger, connectionString, queueName: "wibble"), IAzQueueService
{
}
