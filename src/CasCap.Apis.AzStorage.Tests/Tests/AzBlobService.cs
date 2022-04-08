using CasCap.Services;
using Microsoft.Extensions.Logging;
namespace CasCap.Tests;

public class AzBlobService : AzBlobStorageBase
{
    public AzBlobService(ILogger<AzBlobService> logger) : base(logger, null,//todo: inject config in here for tests
        containerName: "wibble")
    {
    }
}