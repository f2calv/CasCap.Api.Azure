namespace CasCap.Services;

public class AzBlobService : AzBlobStorageBase
{
    public AzBlobService(ILogger<AzBlobService> logger, string connectionString, string containerName)
        : base(logger, connectionString, containerName)
    {
    }
}