namespace CasCap.Messages;

/// <summary>
/// Provides data for the <see cref="CasCap.Abstractions.IAzTableStorageBase.BatchCompletedEvent"/> event,
/// describing the batch that was just processed.
/// </summary>
/// <remarks>
/// Some properties use camelCase naming to preserve backward compatibility with existing consumers.
/// </remarks>
public class AzTableStorageArgs(string storageAccountName, string tableName, string partitionKey, int count, int countRemaining)
{

    /// <summary>Gets or sets the name of the storage account that owns the table.</summary>
    public string StorageAccountName { get; set; } = storageAccountName;

    /// <summary>Gets or sets the name of the table that was written to.</summary>
    public string TableName { get; set; } = tableName;

    /// <summary>Gets or sets the partition key of the entities in this batch.</summary>
    public string PartitionKey { get; set; } = partitionKey;

    /// <summary>Gets or sets the number of entities successfully processed in this batch.</summary>
    public int Count { get; set; } = count;

    /// <summary>Gets or sets the number of entities still remaining to be processed after this batch.</summary>
    public int CountRemaining { get; set; } = countRemaining;

    //public List<T> entities { get; set; }

    /// <summary>Gets or sets the UTC timestamp when this batch completed.</summary>
    public DateTime Time { get; set; } = DateTime.UtcNow;
}
