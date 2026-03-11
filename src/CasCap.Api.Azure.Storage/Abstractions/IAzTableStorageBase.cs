namespace CasCap.Abstractions;

/// <summary>Base abstraction for Azure Table Storage operations.</summary>
public interface IAzTableStorageBase
{
    /// <summary>Raised after a batch operation has completed for a partition.</summary>
    event EventHandler<AzTableStorageArgs> BatchCompletedEvent;

    /// <summary>Returns a list of all tables in the storage account.</summary>
    Task<List<TableItem>> GetTables(CancellationToken cancellationToken);

    /// <summary>Returns a <see cref="Azure.Data.Tables.TableClient" /> for the given table, optionally creating it if it does not exist.</summary>
    Task<TableClient> GetTableClient(string tableName, bool CreateIfNotExists = true, CancellationToken cancellationToken = default);

    /// <summary>Upserts a single entity into the specified table by name.</summary>
    Task<int> UpsertEntity<T>(string tableName, T entity, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>Upserts a single entity into the specified <see cref="Azure.Data.Tables.TableClient" />.</summary>
    Task<int> UpsertEntity<T>(TableClient tbl, T entity, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>Upserts a batch of entities into the specified table by name.</summary>
    Task<List<T>> UploadData<T>(TableClient tbl, List<T> entities, bool useParallelism = true, CancellationToken cancellationToken = default) where T : class, ITableEntity;

    /// <summary>Upserts a batch of entities into the specified <see cref="Azure.Data.Tables.TableClient" />.</summary>
    Task<List<T>> UploadData<T>(string tableName, List<T> entities, bool useParallelism = true, CancellationToken cancellationToken = default) where T : class, ITableEntity;

    /// <summary>Deletes a batch of entities from the specified table by name.</summary>
    Task DeleteData<T>(string tableName, List<T> entities, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>Deletes a batch of entities from the specified <see cref="Azure.Data.Tables.TableClient" />.</summary>
    Task DeleteData<T>(TableClient tbl, List<T> entities, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>Returns a single entity matching the given partition key and optional row key from the specified table by name.</summary>
    Task<T?> GetEntity<T>(string tableName, string partitionKey, string? rowKey = null, CancellationToken cancellationToken = default) where T : class, ITableEntity, new();

    /// <summary>Returns a single entity matching the given partition key and optional row key from the specified <see cref="Azure.Data.Tables.TableClient" />.</summary>
    Task<T?> GetEntity<T>(TableClient tbl, string partitionKey, string? rowKey = null, CancellationToken cancellationToken = default) where T : class, ITableEntity, new();

    /// <summary>Returns all entities from the specified table by name.</summary>
    Task<List<T>> GetEntities<T>(string tableName, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>Returns all entities from the specified <see cref="Azure.Data.Tables.TableClient" />.</summary>
    Task<List<T>> GetEntities<T>(TableClient tbl, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>Returns all entities in the given partition from the specified table by name.</summary>
    Task<List<T>> GetEntities<T>(string tableName, string partitionKey, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>Returns all entities in the given partition from the specified <see cref="Azure.Data.Tables.TableClient" />.</summary>
    Task<List<T>> GetEntities<T>(TableClient tbl, string partitionKey, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>Returns entities in the given partition with row keys newer than <paramref name="rowKeyFrom"/> from the specified table by name.</summary>
    Task<List<T>> GetEntities<T>(string tableName, string partitionKey, string rowKeyFrom, string rowKeyTo, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    /// <summary>Returns entities in the given partition within a row-key range from the specified <see cref="Azure.Data.Tables.TableClient" />.</summary>
    Task<List<T>> GetEntities<T>(TableClient tbl, string partitionKey, string rowKeyFrom, string rowKeyTo, CancellationToken cancellationToken) where T : class, ITableEntity, new();
}
