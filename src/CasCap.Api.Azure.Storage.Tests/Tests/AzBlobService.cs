namespace CasCap.Tests;

public interface IAzBlobService : IAzBlobStorageBase
{
}

public class AzBlobService(string connectionString)
    : AzBlobStorageBase(connectionString, containerName: "wibble"), IAzBlobService
{
}
