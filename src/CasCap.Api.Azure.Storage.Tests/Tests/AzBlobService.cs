namespace CasCap.Tests;

/// <summary>Concrete blob storage service used in integration tests.</summary>
public class AzBlobService(string connectionString, string containerName = "wibble")
    : AzBlobStorageBase(connectionString, containerName), IAzBlobService
{
}
