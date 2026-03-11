namespace CasCap.Abstractions;

/// <summary>
/// Defines the contract for interacting with Azure Table Storage, supporting CRUD operations across one or more tables.
/// </summary>
public interface IAzTableStorageBase
{
    /// <summary>Raised after each batch of entities has been successfully processed.</summary>
    event EventHandler<AzTableStorageArgs> BatchCompletedEvent;

    /// <summary>
    /// Returns a list of all tables in the storage account.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A list of <see cref="TableItem"/> objects describing the available tables.</returns>
    Task<List<TableItem>> GetTables(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a <see cref="TableClient"/> for the specified table, optionally creating the table if it does not exist.
    /// </summary>
    /// <param name="tableName">The name of the table to retrieve.</param>
    /// <param name="CreateIfNotExists">When <see langword="true"/> (the default), the table is created if it does not already exist.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="TableClient"/> for the specified table.</returns>
    Task<TableClient> GetTableClient(string tableName, bool CreateIfNotExists = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts (inserts or replaces) a batch of entities into the specified table.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tbl">The <see cref="TableClient"/> targeting the destination table.</param>
    /// <param name="entities">The list of entities to upsert.</param>
    /// <param name="useParallelism">When <see langword="true"/> (the default), batches are processed in parallel across partitions.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The list of entities that were successfully upserted.</returns>
    Task<List<T>> UploadData<T>(TableClient tbl, List<T> entities, bool useParallelism = true, CancellationToken cancellationToken = default) where T : class, ITableEntity;

    /// <summary>
    /// Upserts (inserts or replaces) a batch of entities into the specified table.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tableName">The name of the table to write to.</param>
    /// <param name="entities">The list of entities to upsert.</param>
    /// <param name="useParallelism">When <see langword="true"/> (the default), batches are processed in parallel across partitions.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The list of entities that were successfully upserted.</returns>
    Task<List<T>> UploadData<T>(string tableName, List<T> entities, bool useParallelism = true, CancellationToken cancellationToken = default) where T : class, ITableEntity;

    /// <summary>
    /// Deletes a batch of entities from the specified table.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tableName">The name of the table from which entities will be deleted.</param>
    /// <param name="entities">The list of entities to delete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    Task DeleteData<T>(string tableName, List<T> entities, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>
    /// Deletes a batch of entities from the table represented by the given <see cref="TableClient"/>.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tbl">The <see cref="TableClient"/> targeting the destination table.</param>
    /// <param name="entities">The list of entities to delete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    Task DeleteData<T>(TableClient tbl, List<T> entities, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>
    /// Upserts a single entity into the specified table.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tableName">The name of the table to write to.</param>
    /// <param name="entity">The entity to upsert.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The HTTP status code returned by the storage service.</returns>
    Task<int> UpsertEntity<T>(string tableName, T entity, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>
    /// Upserts a single entity into the table represented by the given <see cref="TableClient"/>.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tbl">The <see cref="TableClient"/> targeting the destination table.</param>
    /// <param name="entity">The entity to upsert.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The HTTP status code returned by the storage service.</returns>
    Task<int> UpsertEntity<T>(TableClient tbl, T entity, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>
    /// Retrieves a single entity by partition key and optional row key from the specified table.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tableName">The name of the table to query.</param>
    /// <param name="partitionKey">The partition key of the entity to retrieve.</param>
    /// <param name="rowKey">The row key of the entity to retrieve. When <see langword="null"/>, the first entity in the partition is returned.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The matching entity, or <see langword="null"/> if it does not exist.</returns>
    Task<T?> GetEntity<T>(string tableName, string partitionKey, string? rowKey = null, CancellationToken cancellationToken = default) where T : class, ITableEntity, new();

    /// <summary>
    /// Retrieves a single entity by partition key and optional row key from the table represented by the given <see cref="TableClient"/>.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tbl">The <see cref="TableClient"/> targeting the source table.</param>
    /// <param name="partitionKey">The partition key of the entity to retrieve.</param>
    /// <param name="rowKey">The row key of the entity to retrieve. When <see langword="null"/>, the first entity in the partition is returned.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The matching entity, or <see langword="null"/> if it does not exist.</returns>
    Task<T?> GetEntity<T>(TableClient tbl, string partitionKey, string? rowKey = null, CancellationToken cancellationToken = default) where T : class, ITableEntity, new();

    /// <summary>
    /// Retrieves all entities from the specified table.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tableName">The name of the table to query.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A list of all entities in the table.</returns>
    Task<List<T>> GetEntities<T>(string tableName, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>
    /// Retrieves all entities from the table represented by the given <see cref="TableClient"/>.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tbl">The <see cref="TableClient"/> targeting the source table.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A list of all entities in the table.</returns>
    Task<List<T>> GetEntities<T>(TableClient tbl, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>
    /// Retrieves all entities belonging to the specified partition from the given table.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tableName">The name of the table to query.</param>
    /// <param name="partitionKey">The partition key to filter entities by.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A list of entities in the specified partition.</returns>
    Task<List<T>> GetEntities<T>(string tableName, string partitionKey, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>
    /// Retrieves all entities belonging to the specified partition from the table represented by the given <see cref="TableClient"/>.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tbl">The <see cref="TableClient"/> targeting the source table.</param>
    /// <param name="partitionKey">The partition key to filter entities by.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A list of entities in the specified partition.</returns>
    Task<List<T>> GetEntities<T>(TableClient tbl, string partitionKey, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>
    /// Retrieves entities within a row key range from the specified partition of the given table.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tableName">The name of the table to query.</param>
    /// <param name="partitionKey">The partition key to filter entities by.</param>
    /// <param name="rowKeyFrom">The row key lower bound (exclusive) of the range to retrieve.</param>
    /// <param name="rowKeyTo">The row key upper bound (inclusive) of the range to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A list of entities within the specified row key range.</returns>
    Task<List<T>> GetEntities<T>(string tableName, string partitionKey, string rowKeyFrom, string rowKeyTo, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>
    /// Retrieves entities within a row key range from the specified partition using the given <see cref="TableClient"/>.
    /// </summary>
    /// <typeparam name="T">The entity type, which must implement <see cref="ITableEntity"/>.</typeparam>
    /// <param name="tbl">The <see cref="TableClient"/> targeting the source table.</param>
    /// <param name="partitionKey">The partition key to filter entities by.</param>
    /// <param name="rowKeyFrom">The row key lower bound (exclusive) of the range to retrieve.</param>
    /// <param name="rowKeyTo">The row key upper bound (inclusive) of the range to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A list of entities within the specified row key range.</returns>
    Task<List<T>> GetEntities<T>(TableClient tbl, string partitionKey, string rowKeyFrom, string rowKeyTo, CancellationToken cancellationToken) where T : class, ITableEntity, new();
}
