namespace CasCap.Services;

public abstract class AzTableStorageBase : IAzTableStorageBase
{
    protected static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(AzTableStorageBase));

    public event EventHandler<AzTableStorageArgs>? BatchCompletedEvent;
    protected virtual void OnRaiseBatchCompletedEvent(AzTableStorageArgs args) { BatchCompletedEvent?.Invoke(this, args); }

    protected TableServiceClient _tableSvcClient { get; set; }

    protected AzTableStorageBase(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _tableSvcClient = new TableServiceClient(connectionString);
    }

    protected AzTableStorageBase(Uri endpoint, TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(credential);
        _tableSvcClient = new TableServiceClient(endpoint, credential);
    }

    public async Task<List<TableItem>> GetTables(CancellationToken cancellationToken)
        => await _tableSvcClient.QueryAsync(cancellationToken: cancellationToken).ToListAsync(cancellationToken: cancellationToken);

    public async Task<TableClient> GetTableClient(string tableName, bool CreateIfNotExists = true, CancellationToken cancellationToken = default)
    {
        var table = _tableSvcClient.GetTableClient(tableName);
        if (!await _tableSvcClient.ExistsAsync(tableName) && CreateIfNotExists)
            await table.CreateIfNotExistsAsync(cancellationToken);
        return table;
    }

    protected async Task<TableClient> SetActiveTable(string tableName, bool CreateIfNotExists = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName), "expected!");
        var table = _tableSvcClient.GetTableClient(tableName);
        if (!await _tableSvcClient.ExistsAsync(table.Name) && CreateIfNotExists)
            await table.CreateIfNotExistsAsync(cancellationToken);
        return table;
    }

    #region C - Create
    //upsert single record
    public async Task<int> UpsertEntity<T>(string tableName, T entity, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await UpsertEntity<T>(table, entity, cancellationToken);
    }
    public async Task<int> UpsertEntity<T>(TableClient tbl, T entity, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var result = await tbl.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
        return result.Status;
    }

    //upsert batch
    public async Task<List<T>> UploadData<T>(string tableName, List<T> entities, bool useParallelism = true, CancellationToken cancellationToken = default) where T : class, ITableEntity
    {
        var tbl = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await BatchOperation(tbl, entities, useParallelism, cancellationToken: cancellationToken);
    }

    public Task<List<T>> UploadData<T>(TableClient tbl, List<T> entities, bool useParallelism = true, CancellationToken cancellationToken = default) where T : class, ITableEntity
        => BatchOperation(tbl, entities, useParallelism, cancellationToken: cancellationToken);
    #endregion

    private async Task<List<T>> BatchOperation<T>(TableClient tbl, List<T> entities, bool useParallelism = true, bool InsertOrReplace = true, CancellationToken cancellationToken = default) where T : class, ITableEntity
    {
        var partitions = entities.GroupBy(l => l.PartitionKey)
            .Select(g => new
            {
                PartitionKey = g.Key,
                Entities = g.Select(p => p).ToList()
            }).ToList();

        var retval = new List<T>(entities.Count);
        //var batch = new List<TableTransactionAction>();
        var po = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = useParallelism ? Environment.ProcessorCount : 1 };
        await Parallel.ForEachAsync(partitions, po, async (p, ct) =>
        {
            await RunBatches(p.PartitionKey, p.Entities, ct);
        });

        return retval;

        async Task RunBatches(string _partitionKey, List<T> _entities, CancellationToken cancellationToken)
        {
            while (_entities.Count > 0)
            {
                //count how many remaining records
                var count = _entities.Count;
                var batchSize = Math.Min(100, count);
                var top100 = _entities.Take(batchSize).ToList();
                var _retval = await ExecuteBatchOperation(top100, cancellationToken);
                if (_retval.Count > 0)
                {
                    _entities.RemoveRange(0, batchSize);
                    retval!.AddRange(_retval);
                    _logger.LogDebug("{ClassName} account {StorageAccountName}, table {TableName}, partition {Partition}, (1 of {PartitionCount}), {EntityCount} entities handled, {RemainingCount} entities remaining",
                        nameof(AzTableStorageBase), _tableSvcClient.AccountName, tbl.Name, _partitionKey, partitions.Count, _retval.Count, count - batchSize);
                    OnRaiseBatchCompletedEvent(new AzTableStorageArgs(_tableSvcClient.AccountName, tbl.Name, _partitionKey, _retval.Count, count - batchSize));
                }
                else
                {
                    _logger.LogWarning("{ClassName} table {TableName}, partition {Partition} no changes affected...",
                        nameof(AzTableStorageBase), tbl.Name, _partitionKey);
                    //return;
                }
            }
        }

        async Task<List<T>> ExecuteBatchOperation(List<T> entityRows, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(tbl);
            if (entityRows.IsNullOrEmpty()) throw new ArgumentNullException(nameof(entityRows));
            IEnumerable<TableTransactionAction> tableTxnRows;
            if (InsertOrReplace)
                tableTxnRows = entityRows.Select(p => new TableTransactionAction(TableTransactionActionType.UpsertReplace, p));
            else
            {
                tableTxnRows = entityRows.Select(p => new TableTransactionAction(TableTransactionActionType.Delete, p));

                var newTableTxnRows = new List<TableTransactionAction>();
                foreach (var ent in tableTxnRows)
                {
                    var exists = await tbl.GetEntityIfExistsAsync<T>(ent.Entity.PartitionKey, ent.Entity.RowKey, cancellationToken: cancellationToken);
                    if (exists.HasValue)
                        newTableTxnRows.Add(ent);
                }
                if (newTableTxnRows.Count > 0)
                    tableTxnRows = newTableTxnRows;
            }
            //batch.AddRange(tableTxnRows);
            try
            {
                var response = await tbl.SubmitTransactionAsync(tableTxnRows, cancellationToken).ConfigureAwait(false);
                for (var i = 0; i < tableTxnRows.Count(); i++)
                {
                    var etag = response.Value[i].Headers.ETag;
                    if (etag.HasValue)
                        entityRows[i].ETag = etag.Value;
                }
                return entityRows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} tableName={TableName}, entities.Count={Count}",
                    nameof(AzTableStorageBase), tbl.Name, entityRows.Count);
                throw;
            }
        }
    }

    #region R - Read
    //single record
    public async Task<T?> GetEntity<T>(string tableName, string partitionKey, string? rowKey = null, CancellationToken cancellationToken = default) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await GetEntity<T>(table, partitionKey, rowKey, cancellationToken);
    }
    public async Task<T?> GetEntity<T>(TableClient tbl, string partitionKey, string? rowKey = null, CancellationToken cancellationToken = default) where T : class, ITableEntity, new()
    {
        if (string.IsNullOrWhiteSpace(rowKey))
            throw new NotImplementedException("TODO: need to create an ODATA query for TOP 1 operations...");
        var result = await tbl.GetEntityIfExistsAsync<T>(partitionKey, rowKey, cancellationToken: cancellationToken);
        if (result.HasValue)
            return result.Value;
        return null;
    }

    //get everything
    public async Task<List<T>> GetEntities<T>(string tableName, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await GetEntities<T>(table, cancellationToken);
    }
    public async Task<List<T>> GetEntities<T>(TableClient tbl, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var tableEntities = await tbl.QueryAsync<T>(cancellationToken: cancellationToken).ToListAsync(cancellationToken: cancellationToken);
        return tableEntities;
    }

    //get just the partition
    public async Task<List<T>> GetEntities<T>(string tableName, string partitionKey, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await GetEntities<T>(table, partitionKey, cancellationToken);
    }
    public async Task<List<T>> GetEntities<T>(TableClient tbl, string partitionKey, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var tableEntities = await tbl.QueryAsync<T>(p => p.PartitionKey == partitionKey, cancellationToken: cancellationToken).ToListAsync(cancellationToken: cancellationToken);
        return tableEntities;
    }
    //https://learn.microsoft.com/en-gb/rest/api/storageservices/querying-tables-and-entities

    //get just the partition AND newer rowkeys within that partition
    public async Task<List<T>> GetEntities<T>(string tableName, string partitionKey, string rowKeyFrom, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await GetEntities<T>(table, partitionKey, rowKeyFrom, cancellationToken);
    }
    public static async Task<List<T>> GetEntities<T>(TableClient table, string partitionKey, string rowKeyFrom, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey} and RowKey lt {rowKeyFrom}");
        var entities = await table.QueryAsync<T>(filter, cancellationToken: cancellationToken).ToListAsync(cancellationToken: cancellationToken);
        return entities.ToList();
    }

    //get a range within a partition
    public async Task<List<T>> GetEntities<T>(string tableName, string partitionKey, string rowKeyFrom, string rowKeyTo, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var tbl = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await GetEntities<T>(tbl, partitionKey, rowKeyFrom, rowKeyTo, cancellationToken);
    }
    public async Task<List<T>> GetEntities<T>(TableClient tbl, string partitionKey, string rowKeyFrom, string rowKeyTo, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey} and RowKey lt {rowKeyFrom} and RowKey ge {rowKeyTo}");
        var entities = await tbl.QueryAsync<T>(filter, cancellationToken: cancellationToken).ToListAsync(cancellationToken: cancellationToken);
        return entities.ToList();
    }
    #endregion

    #region U - Update
    //Note: now using Upsert & Replace
    #endregion

    #region D - Delete
    public async Task DeleteTable(string tableName, CancellationToken cancellationToken)
    {
        if (await _tableSvcClient.ExistsAsync(tableName))
        {
            var response = await _tableSvcClient.DeleteTableAsync(tableName, cancellationToken);
            _logger.LogDebug("{ClassName} {TableName} {ReasonPhrase}", nameof(AzTableStorageBase), tableName, response.ReasonPhrase);
        }
    }

    public async Task DeleteData<T>(string tableName, List<T> entities, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var tbl = await GetTableClient(tableName, cancellationToken: cancellationToken);
        await BatchOperation(tbl, entities, useParallelism: true, InsertOrReplace: false, cancellationToken: cancellationToken);
    }

    public Task DeleteData<T>(TableClient tbl, List<T> entities, CancellationToken cancellationToken) where T : class, ITableEntity, new()
        => BatchOperation(tbl, entities, InsertOrReplace: false, cancellationToken: cancellationToken);
    #endregion
}
