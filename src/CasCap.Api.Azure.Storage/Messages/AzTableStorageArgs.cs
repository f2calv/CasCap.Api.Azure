namespace CasCap.Messages;

/// <summary>
/// Provides data for the <see cref="CasCap.Abstractions.IAzTableStorageBase.BatchCompletedEvent"/> event,
/// describing the batch that was just processed.
/// </summary>
/// <remarks>
/// Some properties use camelCase naming to preserve backward compatibility with existing consumers.
/// </remarks>
public class AzTableStorageArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="AzTableStorageArgs"/> with details about the completed batch.
    /// </summary>
    /// <param name="storageAccountName">The name of the storage account that owns the table.</param>
    /// <param name="tableName">The name of the table that was written to.</param>
    /// <param name="partitionKey">The partition key of the entities in this batch.</param>
    /// <param name="count">The number of entities successfully processed in this batch.</param>
    /// <param name="countRemaining">The number of entities still remaining to be processed.</param>
    public AzTableStorageArgs(string storageAccountName, string tableName, string partitionKey, int count, int countRemaining)
    {
        StorageAccountName = storageAccountName;
        TableName = tableName;
        PartitionKey = partitionKey;
        Count = count;
        CountRemaining = countRemaining;
        Time = DateTime.UtcNow;
    }

    /// <summary>Gets or sets the name of the storage account that owns the table.</summary>
    public string storageAccountName { get; set; }
    /// <summary>Gets or sets the name of the table that was written to.</summary>
    public string tableName { get; set; }
    /// <summary>Gets or sets the partition key of the entities in this batch.</summary>
    public string partitionKey { get; set; }
    /// <summary>Gets or sets the number of entities successfully processed in this batch.</summary>
    public int Count { get; set; }
    /// <summary>Gets or sets the number of entities still remaining to be processed after this batch.</summary>
    public int CountRemaining { get; set; }
    //public List<T> entities { get; set; }
    /// <summary>Gets or sets the UTC timestamp when this batch completed.</summary>
    public DateTime time { get; set; }
}
