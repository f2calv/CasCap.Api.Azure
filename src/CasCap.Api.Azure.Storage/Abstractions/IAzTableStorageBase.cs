namespace CasCap.Abstractions;

public interface IAzTableStorageBase
{
    event EventHandler<AzTableStorageArgs> BatchCompletedEvent;

    Task<List<TableItem>> GetTables(CancellationToken cancellationToken);
    Task<TableClient> GetTableClient(string tableName, bool CreateIfNotExists = true, CancellationToken cancellationToken = default);
    Task<List<T>> UploadData<T>(TableClient tbl, List<T> entities, bool useParallelism = true, CancellationToken cancellationToken = default) where T : class, ITableEntity;
    Task<List<T>> UploadData<T>(string tableName, List<T> entities, bool useParallelism = true, CancellationToken cancellationToken = default) where T : class, ITableEntity;

    Task DeleteData<T>(string tableName, List<T> entities, CancellationToken cancellationToken) where T : class, ITableEntity, new();
    Task DeleteData<T>(TableClient tbl, List<T> entities, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    Task<int> UpsertEntity<T>(string tableName, T entity, CancellationToken cancellationToken) where T : class, ITableEntity, new();
    Task<int> UpsertEntity<T>(TableClient tbl, T entity, CancellationToken cancellationToken) where T : class, ITableEntity, new();

    Task<T?> GetEntity<T>(string tableName, string partitionKey, string? rowKey = null, CancellationToken cancellationToken = default) where T : class, ITableEntity, new();
    Task<T?> GetEntity<T>(TableClient tbl, string partitionKey, string? rowKey = null, CancellationToken cancellationToken = default) where T : class, ITableEntity, new();

    Task<List<T>> GetEntities<T>(string tableName, CancellationToken cancellationToken) where T : class, ITableEntity, new();
    Task<List<T>> GetEntities<T>(TableClient tbl, CancellationToken cancellationToken) where T : class, ITableEntity, new();
    Task<List<T>> GetEntities<T>(string tableName, string partitionKey, CancellationToken cancellationToken) where T : class, ITableEntity, new();
    Task<List<T>> GetEntities<T>(TableClient tbl, string partitionKey, CancellationToken cancellationToken) where T : class, ITableEntity, new();
    Task<List<T>> GetEntities<T>(string tableName, string partitionKey, string rowKeyFrom, string rowKeyTo, CancellationToken cancellationToken) where T : class, ITableEntity, new();
    Task<List<T>> GetEntities<T>(TableClient tbl, string partitionKey, string rowKeyFrom, string rowKeyTo, CancellationToken cancellationToken) where T : class, ITableEntity, new();
}
