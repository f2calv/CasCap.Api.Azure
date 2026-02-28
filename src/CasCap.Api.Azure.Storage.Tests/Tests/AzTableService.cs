namespace CasCap.Tests;

public interface IAzTableService : IAzTableStorageBase
{
}

public class AzTableService(string connectionString) : AzTableStorageBase(connectionString), IAzTableService
{
}
