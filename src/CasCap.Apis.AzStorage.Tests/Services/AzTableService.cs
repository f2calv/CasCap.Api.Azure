namespace CasCap.Services;

public class AzTableService : AzTableStorageBase
{
    public AzTableService(ILogger<AzTableService> logger, string connectionString)
        : base(logger, connectionString)
    {
    }
}