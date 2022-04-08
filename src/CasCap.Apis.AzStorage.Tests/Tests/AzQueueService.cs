using CasCap.Services;
using Microsoft.Extensions.Logging;
namespace CasCap.Tests;

public interface IAzQueueService : IAzQueueStorageBase
{
}

public class AzQueueService : AzQueueStorageBase, IAzQueueService
{
    public AzQueueService(ILogger<AzQueueService> logger) : base(logger, null,//todo: inject config in here for tests
        queueName: "wibble")
    {
    }
}