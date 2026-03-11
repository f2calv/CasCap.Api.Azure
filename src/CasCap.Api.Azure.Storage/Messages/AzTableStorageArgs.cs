namespace CasCap.Messages;

/// <summary>Event arguments raised after an Azure Table Storage batch operation completes.</summary>
public record AzTableStorageArgs
{
    /// <summary>Initializes a new instance of <see cref="AzTableStorageArgs" />.</summary>
    public AzTableStorageArgs(string storageAccountName, string tableName, string partitionKey, int count, int countRemaining)
    {
        StorageAccountName = storageAccountName;
        TableName = tableName;
        PartitionKey = partitionKey;
        Count = count;
        CountRemaining = countRemaining;
        Time = DateTime.UtcNow;
    }

    /// <summary>Gets the name of the storage account.</summary>
    public string StorageAccountName { get; init; }

    /// <summary>Gets the name of the table.</summary>
    public string TableName { get; init; }

    /// <summary>Gets the partition key of the batch that was processed.</summary>
    public string PartitionKey { get; init; }

    /// <summary>Gets the number of entities processed in this batch.</summary>
    public int Count { get; init; }

    /// <summary>Gets the number of entities still remaining to be processed.</summary>
    public int CountRemaining { get; init; }

    /// <summary>Gets the UTC timestamp when the batch completed.</summary>
    public DateTime Time { get; init; }
}
