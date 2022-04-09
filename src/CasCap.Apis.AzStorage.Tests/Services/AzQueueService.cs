namespace CasCap.Services;

public class AzQueueService : AzQueueStorageBase
{
    public AzQueueService(ILogger<AzQueueService> logger, string connectionString, string queueName)
        : base(logger, connectionString, queueName)
    {
    }
}