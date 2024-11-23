namespace CasCap.Tests;

public interface IAzBlobService : IAzBlobStorageBase
{
}

public class AzBlobService(ILogger<AzBlobService> logger, string connectionString)
    : AzBlobStorageBase(logger, connectionString, containerName: "wibble"), IAzBlobService
{
}
