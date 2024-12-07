namespace CasCap.Services;

public interface IAzTableStorageBase
{
    event EventHandler<AzTableStorageArgs> BatchCompletedEvent;

    Task<List<TableItem>> GetTables();
    Task<TableClient> GetTableClient(string tableName, bool CreateIfNotExists = true);
    Task<List<T>> UploadData<T>(TableClient tbl, List<T> entities, bool useParallelism = true) where T : class, ITableEntity;
    Task<List<T>> UploadData<T>(string tableName, List<T> entities, bool useParallelism = true) where T : class, ITableEntity;

    Task DeleteData<T>(string tableName, List<T> entities) where T : class, ITableEntity, new();
    Task DeleteData<T>(TableClient tbl, List<T> entities) where T : class, ITableEntity, new();

    Task<int> UpsertEntity<T>(string tableName, T entity) where T : class, ITableEntity, new();
    Task<int> UpsertEntity<T>(TableClient tbl, T entity) where T : class, ITableEntity, new();

    Task<T> GetEntity<T>(string tableName, string partitionKey, string? rowKey = null) where T : class, ITableEntity, new();
    Task<T> GetEntity<T>(TableClient tbl, string partitionKey, string? rowKey = null) where T : class, ITableEntity, new();

    Task<List<T>> GetEntities<T>(string tableName) where T : class, ITableEntity, new();
    Task<List<T>> GetEntities<T>(TableClient tbl) where T : class, ITableEntity, new();
    Task<List<T>> GetEntities<T>(string tableName, string partitionKey) where T : class, ITableEntity, new();
    Task<List<T>> GetEntities<T>(TableClient tbl, string partitionKey) where T : class, ITableEntity, new();
    Task<List<T>> GetEntities<T>(string tableName, string partitionKey, string rowKeyFrom, string rowKeyTo) where T : class, ITableEntity, new();
    Task<List<T>> GetEntities<T>(TableClient tbl, string partitionKey, string rowKeyFrom, string rowKeyTo) where T : class, ITableEntity, new();
}

public abstract class AzTableStorageBase : IAzTableStorageBase
{
    protected readonly ILogger _logger;

    public event EventHandler<AzTableStorageArgs>? BatchCompletedEvent;
    protected virtual void OnRaiseBatchCompletedEvent(AzTableStorageArgs args) { BatchCompletedEvent?.Invoke(this, args); }

    private readonly string _connectionString;

    protected TableServiceClient _tableSvcClient { get; set; }

    public AzTableStorageBase(ILogger<AzTableStorageBase> logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString ?? throw new ArgumentException("not supplied!", nameof(connectionString));

        _tableSvcClient = new TableServiceClient(_connectionString);
    }

    public async Task<List<TableItem>> GetTables() => await _tableSvcClient.QueryAsync().ToListAsync();

    public async Task<TableClient> GetTableClient(string tableName, bool CreateIfNotExists = true)
    {
        var table = _tableSvcClient.GetTableClient(tableName);
        if (!await _tableSvcClient.ExistsAsync(tableName) && CreateIfNotExists)
            await table.CreateIfNotExistsAsync();
        return table;
    }

    protected async Task<TableClient> SetActiveTable(string tableName, bool CreateIfNotExists = true)
    {
        if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName), "expected!");
        var table = _tableSvcClient.GetTableClient(tableName);
        if (!await _tableSvcClient.ExistsAsync(table.Name) && CreateIfNotExists) await table.CreateIfNotExistsAsync();
        return table;
    }

    #region C - Create
    //upsert single record
    public async Task<int> UpsertEntity<T>(string tableName, T entity) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName);
        return await UpsertEntity<T>(table, entity);
    }
    public async Task<int> UpsertEntity<T>(TableClient table, T tableEntity) where T : class, ITableEntity, new()
    {
        var result = await table.UpsertEntityAsync(tableEntity);
        return result.Status;
    }

    //upsert batch
    public async Task<List<T>> UploadData<T>(string tableName, List<T> tableEntities, bool useParallelism = true) where T : class, ITableEntity
    {
        var tbl = await GetTableClient(tableName);
        return await BatchOperation(tbl, tableEntities, useParallelism);
    }

    public Task<List<T>> UploadData<T>(TableClient tbl, List<T> entities, bool useParallelism = true) where T : class, ITableEntity => BatchOperation(tbl, entities, useParallelism);
    #endregion

    private async Task<List<T>> BatchOperation<T>(TableClient tbl, List<T> entities, bool useParallelism = true, bool InsertOrReplace = true) where T : class, ITableEntity
    {
        var partitions = entities.GroupBy(l => l.PartitionKey)
            .Select(g => new
            {
                PartitionKey = g.Key,
                Entities = g.Select(p => p).ToList()
            }).ToList();

        var retval = new List<T>(entities.Count);
        //var batch = new List<TableTransactionAction>();
        await Parallel.ForEachAsync(partitions, new ParallelOptions { MaxDegreeOfParallelism = useParallelism ? Environment.ProcessorCount : 1 },
            async (p, ct) =>
            {
                //TODO: use CancellationToken (ct) where appropriate
                await RunBatches(p.PartitionKey, p.Entities);
            });

        return retval;

        async Task RunBatches(string _partitionKey, List<T> _entities)
        {
            while (_entities.Count > 0)
            {
                //count how many remaining records
                var count = _entities.Count;
                var batchSize = Math.Min(100, count);
                var top100 = _entities.Take(batchSize).ToList();
                var _retval = await ExecuteBatchOperation(top100);
                if (_retval.Count > 0)
                {
                    _entities.RemoveRange(0, batchSize);
                    retval!.AddRange(_retval);
                    _logger.LogDebug("account {storageAccountName}, table {tableName}, partition {partition}, (1 of {partitionCount}), {entityCount} entities handled, {remainingCount} entities remaining",
                        _tableSvcClient.AccountName, tbl.Name, _partitionKey, partitions.Count, _retval.Count, count - batchSize);
                    OnRaiseBatchCompletedEvent(new AzTableStorageArgs(_tableSvcClient.AccountName, tbl.Name, _partitionKey, _retval.Count, count - batchSize));
                }
                else
                {
                    _logger.LogWarning("table {tableName}, partition {partition} no changes affected...", tbl.Name, _partitionKey);
                    //return;
                }
            }
        }

        async Task<List<T>> ExecuteBatchOperation(List<T> entityRows)
        {
            if (tbl is null) throw new ArgumentNullException(nameof(tbl));
            if (entityRows.IsNullOrEmpty()) throw new ArgumentNullException(nameof(entityRows));
            var retval = new List<T>();
            IEnumerable<TableTransactionAction> tableTxnRows;
            if (InsertOrReplace)
                tableTxnRows = entityRows.Select(p => new TableTransactionAction(TableTransactionActionType.UpsertReplace, p));
            else
            {
                tableTxnRows = entityRows.Select(p => new TableTransactionAction(TableTransactionActionType.Delete, p));

                var newTableTxnRows = new List<TableTransactionAction>();
                foreach (var ent in tableTxnRows)
                {
                    var exists = await tbl.GetEntityIfExistsAsync<T>(ent.Entity.PartitionKey, ent.Entity.RowKey);
                    if (exists.HasValue)
                        newTableTxnRows.Add(ent);
                }
                if (newTableTxnRows.Any())
                    tableTxnRows = newTableTxnRows;
            }
            //batch.AddRange(tableTxnRows);
            try
            {
                var response = await tbl.SubmitTransactionAsync(tableTxnRows).ConfigureAwait(false);
                for (var i = 0; i < tableTxnRows.Count(); i++)
                {
                    var etag = response.Value[i].Headers.ETag;
                    if (etag.HasValue)
                        entityRows[i].ETag = etag.Value;
                }
                retval = entityRows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "tableName={tableName}, entities.Count={count}", tbl.Name, entityRows.Count);
                throw;
            }
            return retval;
        }
    }

    #region R - Read
    //single record
    public async Task<T> GetEntity<T>(string tableName, string partitionKey, string? rowKey = null) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName);
        return await GetEntity<T>(table, partitionKey, rowKey);
    }
    public async Task<T> GetEntity<T>(TableClient table, string partitionKey, string? rowKey = null) where T : class, ITableEntity, new()
    {
        if (string.IsNullOrWhiteSpace(rowKey))
            throw new NotImplementedException("TODO: need to create an ODATA query for TOP 1 operations...");
        var result = await table.GetEntityIfExistsAsync<T>(partitionKey, rowKey);
        if (result.HasValue)
            return result.Value;
        return null;
    }

    //get everything
    public async Task<List<T>> GetEntities<T>(string tableName) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName);
        return await GetEntities<T>(table);
    }
    public async Task<List<T>> GetEntities<T>(TableClient table) where T : class, ITableEntity, new()
    {
        var tableEntities = await table.QueryAsync<T>().ToListAsync();
        return tableEntities;
    }

    //get just the partition
    public async Task<List<T>> GetEntities<T>(string tableName, string partitionKey) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName);
        return await GetEntities<T>(table, partitionKey);
    }
    public async Task<List<T>> GetEntities<T>(TableClient table, string partitionKey) where T : class, ITableEntity, new()
    {
        var tableEntities = await table.QueryAsync<T>(p => p.PartitionKey == partitionKey).ToListAsync();
        return tableEntities;
    }
    //https://learn.microsoft.com/en-gb/rest/api/storageservices/querying-tables-and-entities

    //get just the partition AND newer rowkeys within that partition
    public async Task<List<T>> GetEntities<T>(string tableName, string partitionKey, string rowKeyFrom) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName);
        return await GetEntities<T>(table, partitionKey, rowKeyFrom);
    }
    public async Task<List<T>> GetEntities<T>(TableClient table, string partitionKey, string rowKeyFrom) where T : class, ITableEntity, new()
    {
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey} and RowKey lt {rowKeyFrom}");
        var entities = await table.QueryAsync<T>(filter).ToListAsync();
        return entities.ToList();
    }

    //get a range within a partition
    public async Task<List<T>> GetEntities<T>(string tableName, string partitionKey, string rowKeyFrom, string rowKeyTo) where T : class, ITableEntity, new()
    {
        var tbl = await GetTableClient(tableName);
        return await GetEntities<T>(tbl, partitionKey, rowKeyFrom, rowKeyTo);
    }
    public async Task<List<T>> GetEntities<T>(TableClient table, string partitionKey, string rowKeyFrom, string rowKeyTo) where T : class, ITableEntity, new()
    {
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey} and RowKey lt {rowKeyFrom} and RowKey ge {rowKeyTo}");
        var entities = await table.QueryAsync<T>(filter).ToListAsync();
        return entities.ToList();
    }
    #endregion

    #region U - Update
    //Note: now using Upsert & Replace
    #endregion

    #region D - Delete
    public async Task DeleteTable(string tableName)
    {
        if (await _tableSvcClient.ExistsAsync(tableName))
        {
            var response = await _tableSvcClient.DeleteTableAsync(tableName);
            _logger.LogDebug("{tableName} {ReasonPhrase}", tableName, response.ReasonPhrase);
        }
    }

    public async Task DeleteData<T>(string tableName, List<T> entities) where T : class, ITableEntity, new()
    {
        var tbl = await GetTableClient(tableName);
        await BatchOperation(tbl, entities, useParallelism: true, InsertOrReplace: false);
    }

    public Task DeleteData<T>(TableClient tbl, List<T> entities) where T : class, ITableEntity, new() => BatchOperation(tbl, entities, InsertOrReplace: false);
    #endregion
}
