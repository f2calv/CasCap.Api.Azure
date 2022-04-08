namespace CasCap.Messages;

public class AzTableStorageArgs
{
    public AzTableStorageArgs(string storageAccountName, string tableName, string partitionKey, int count, int countRemaining)
    {
        this.storageAccountName = storageAccountName;
        this.tableName = tableName;
        this.partitionKey = partitionKey;
        Count = count;
        CountRemaining = countRemaining;
        time = DateTime.UtcNow;
    }

    public string storageAccountName { get; set; }
    public string tableName { get; set; }
    public string partitionKey { get; set; }
    public int Count { get; set; }
    public int CountRemaining { get; set; }
    //public List<T> entities { get; set; }
    public DateTime time { get; set; }
}