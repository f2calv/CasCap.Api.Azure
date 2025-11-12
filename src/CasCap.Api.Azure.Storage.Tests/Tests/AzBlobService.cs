namespace CasCap.Tests;

public interface IAzBlobService : IAzBlobStorageBase
{
}

public class AzBlobService(string connectionString, string containerName= "wibble")
    : AzBlobStorageBase(connectionString, containerName), IAzBlobService
{
}
