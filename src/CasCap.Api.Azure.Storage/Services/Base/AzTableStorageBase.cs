namespace CasCap.Services;

/// <inheritdoc/>
public abstract class AzTableStorageBase : IAzTableStorageBase
{
    /// <summary>Logger instance for this class.</summary>
    protected static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(AzTableStorageBase));

    /// <inheritdoc/>
    public event EventHandler<AzTableStorageArgs>? BatchCompletedEvent;

    /// <summary>Raises the <see cref="BatchCompletedEvent"/> event.</summary>
    protected virtual void OnRaiseBatchCompletedEvent(AzTableStorageArgs args)
        => BatchCompletedEvent?.Invoke(this, args);

    /// <summary>Gets or sets the underlying <see cref="TableServiceClient" />.</summary>
    protected TableServiceClient _tableSvcClient { get; set; }

    /// <summary>Initializes a new instance of <see cref="AzTableStorageBase" /> using a connection string.</summary>
    protected AzTableStorageBase(string connectionString, TableClientOptions.ServiceVersion? serviceVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _tableSvcClient = serviceVersion.HasValue
            ? new TableServiceClient(connectionString, new TableClientOptions(serviceVersion.Value))
            : new TableServiceClient(connectionString);
    }

    /// <summary>Initializes a new instance of <see cref="AzTableStorageBase" /> using a service endpoint and token credential.</summary>
    protected AzTableStorageBase(Uri endpoint, TokenCredential credential, TableClientOptions.ServiceVersion? serviceVersion = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(credential);
        _tableSvcClient = serviceVersion.HasValue
            ? new TableServiceClient(endpoint, credential, new TableClientOptions(serviceVersion.Value))
            : new TableServiceClient(endpoint, credential);
    }

    /// <inheritdoc/>
    public Task<List<TableItem>> GetTables(CancellationToken cancellationToken)
        => _tableSvcClient.QueryAsync(cancellationToken: cancellationToken).ToListAsync(cancellationToken: cancellationToken).AsTask();

    /// <inheritdoc/>
    public async Task<TableClient> GetTableClient(string tableName, bool CreateIfNotExists = true, CancellationToken cancellationToken = default)
    {
        var table = _tableSvcClient.GetTableClient(tableName);
        if (!await _tableSvcClient.ExistsAsync(tableName) && CreateIfNotExists)
            await table.CreateIfNotExistsAsync(cancellationToken);
        return table;
    }

    /// <summary>Sets the active <see cref="TableClient"/> for <paramref name="tableName"/>, optionally creating it if it does not exist.</summary>
    protected async Task<TableClient> SetActiveTable(string tableName, bool CreateIfNotExists = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        var table = _tableSvcClient.GetTableClient(tableName);
        if (!await _tableSvcClient.ExistsAsync(table.Name) && CreateIfNotExists)
            await table.CreateIfNotExistsAsync(cancellationToken);
        return table;
    }

    #region C - Create
    /// <inheritdoc/>
    public async Task<int> UpsertEntity<T>(string tableName, T entity, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await UpsertEntity<T>(table, entity, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> UpsertEntity<T>(TableClient tbl, T entity, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var result = await tbl.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
        return result.Status;
    }

    /// <inheritdoc/>
    public async Task<List<T>> UploadData<T>(string tableName, List<T> entities, bool useParallelism = true, CancellationToken cancellationToken = default) where T : class, ITableEntity
    {
        var tbl = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await BatchOperation(tbl, entities, useParallelism, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public Task<List<T>> UploadData<T>(TableClient tbl, List<T> entities, bool useParallelism = true, CancellationToken cancellationToken = default) where T : class, ITableEntity
        => BatchOperation(tbl, entities, useParallelism, cancellationToken: cancellationToken);
    #endregion

    private async Task<List<T>> BatchOperation<T>(TableClient tbl, List<T> entities, bool useParallelism = true, bool InsertOrReplace = true, CancellationToken cancellationToken = default) where T : class, ITableEntity
    {
        var partitions = entities.GroupBy(e => e.PartitionKey)
            .Select(g => new
            {
                PartitionKey = g.Key,
                Entities = g.ToList()
            }).ToList();

        var retval = new ConcurrentBag<T>();
        var po = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = useParallelism ? Environment.ProcessorCount : 1 };
        await Parallel.ForEachAsync(partitions, po, async (p, ct) =>
        {
            await RunBatches(p.PartitionKey, p.Entities, ct);
        });

        return retval.ToList();

        async Task RunBatches(string partitionKey, List<T> partitionEntities, CancellationToken cancellationToken)
        {
            while (partitionEntities.Count > 0)
            {
                var count = partitionEntities.Count;
                var batchSize = Math.Min(100, count);
                var batch = partitionEntities.Take(batchSize).ToList();
                var batchResult = await ExecuteBatchOperation(batch, cancellationToken);
                if (batchResult.Count > 0)
                {
                    partitionEntities.RemoveRange(0, batchSize);
                    foreach (var item in batchResult)
                        retval.Add(item);
                    _logger.LogDebug("{ClassName} account {StorageAccountName}, table {TableName}, partition {Partition}, (1 of {PartitionCount}), {EntityCount} entities handled, {RemainingCount} entities remaining",
                        nameof(AzTableStorageBase), _tableSvcClient.AccountName, tbl.Name, partitionKey, partitions.Count, batchResult.Count, count - batchSize);
                    OnRaiseBatchCompletedEvent(new AzTableStorageArgs(_tableSvcClient.AccountName, tbl.Name, partitionKey, batchResult.Count, count - batchSize));
                }
                else
                    _logger.LogWarning("{ClassName} table {TableName}, partition {Partition} no changes affected...",
                        nameof(AzTableStorageBase), tbl.Name, partitionKey);
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
    /// <inheritdoc/>
    public async Task<T?> GetEntity<T>(string tableName, string partitionKey, string? rowKey = null, CancellationToken cancellationToken = default) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await GetEntity<T>(table, partitionKey, rowKey, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<T?> GetEntity<T>(TableClient tbl, string partitionKey, string? rowKey = null, CancellationToken cancellationToken = default) where T : class, ITableEntity, new()
    {
        if (string.IsNullOrWhiteSpace(rowKey))
            throw new NotImplementedException("TODO: need to create an ODATA query for TOP 1 operations...");
        var result = await tbl.GetEntityIfExistsAsync<T>(partitionKey, rowKey, cancellationToken: cancellationToken);
        if (result.HasValue)
            return result.Value;
        return null;
    }

    /// <inheritdoc/>
    public async Task<List<T>> GetEntities<T>(string tableName, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await GetEntities<T>(table, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<List<T>> GetEntities<T>(TableClient tbl, CancellationToken cancellationToken) where T : class, ITableEntity, new()
        => tbl.QueryAsync<T>(cancellationToken: cancellationToken).ToListAsync(cancellationToken: cancellationToken).AsTask();

    /// <inheritdoc/>
    public async Task<List<T>> GetEntities<T>(string tableName, string partitionKey, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await GetEntities<T>(table, partitionKey, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>See <see href="https://learn.microsoft.com/en-gb/rest/api/storageservices/querying-tables-and-entities" />.</remarks>
    public Task<List<T>> GetEntities<T>(TableClient tbl, string partitionKey, CancellationToken cancellationToken) where T : class, ITableEntity, new()
        => tbl.QueryAsync<T>(p => p.PartitionKey == partitionKey, cancellationToken: cancellationToken).ToListAsync(cancellationToken: cancellationToken).AsTask();

    /// <inheritdoc/>
    public async Task<List<T>> GetEntities<T>(string tableName, string partitionKey, string rowKeyFrom, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var table = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await GetEntities<T>(table, partitionKey, rowKeyFrom, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<T>> GetEntities<T>(TableClient table, string partitionKey, string rowKeyFrom, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey} and RowKey lt {rowKeyFrom}");
        return await table.QueryAsync<T>(filter, cancellationToken: cancellationToken).ToListAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<T>> GetEntities<T>(string tableName, string partitionKey, string rowKeyFrom, string rowKeyTo, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var tbl = await GetTableClient(tableName, cancellationToken: cancellationToken);
        return await GetEntities<T>(tbl, partitionKey, rowKeyFrom, rowKeyTo, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<T>> GetEntities<T>(TableClient tbl, string partitionKey, string rowKeyFrom, string rowKeyTo, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey} and RowKey lt {rowKeyFrom} and RowKey ge {rowKeyTo}");
        return await tbl.QueryAsync<T>(filter, cancellationToken: cancellationToken).ToListAsync(cancellationToken: cancellationToken);
    }
    #endregion

    #region U - Update
    //Note: now using Upsert & Replace
    #endregion

    #region D - Delete
    /// <summary>Deletes the specified table if it exists.</summary>
    public async Task DeleteTable(string tableName, CancellationToken cancellationToken)
    {
        if (await _tableSvcClient.ExistsAsync(tableName))
        {
            var response = await _tableSvcClient.DeleteTableAsync(tableName, cancellationToken);
            _logger.LogDebug("{ClassName} {TableName} {ReasonPhrase}", nameof(AzTableStorageBase), tableName, response.ReasonPhrase);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteData<T>(string tableName, List<T> entities, CancellationToken cancellationToken) where T : class, ITableEntity, new()
    {
        var tbl = await GetTableClient(tableName, cancellationToken: cancellationToken);
        await BatchOperation(tbl, entities, useParallelism: true, InsertOrReplace: false, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public Task DeleteData<T>(TableClient tbl, List<T> entities, CancellationToken cancellationToken) where T : class, ITableEntity, new()
        => BatchOperation(tbl, entities, InsertOrReplace: false, cancellationToken: cancellationToken);
    #endregion
}
