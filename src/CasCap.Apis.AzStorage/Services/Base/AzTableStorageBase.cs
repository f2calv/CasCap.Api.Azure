using CasCap.Common.Extensions;
using CasCap.Messages;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
namespace CasCap.Services;

public interface IAzTableStorageBase
{
    event EventHandler<AzTableStorageArgs> BatchCompletedEvent;

    List<CloudTable> GetTables();
    Task<CloudTable> GetTblRef(string tableName, bool CreateIfNotExists = true);
    Task<List<T>> UploadData<T>(CloudTable tbl, List<T> entities, bool verbose = false, bool useParallelism = true) where T : ITableEntity;
    Task<List<T>> UploadData<T>(string tableName, List<T> entities, bool verbose = false, bool useParallelism = true) where T : ITableEntity;
    //Task<List<T>> UploadBatch<T>(CloudTable tbl, List<T> entities);
    //Task<List<T>> UploadBatch<T>(string tableName, List<T> entities);

    Task DeleteData<T>(string tableName, List<T> entities) where T : ITableEntity, new();
    //Task DeleteData<T>(CloudTable tbl, List<T> entities) where T : ITableEntity, new();

    Task<T> UpsertEntity<T>(string tableName, T entity) where T : ITableEntity, new();
    Task<T> UpsertEntity<T>(CloudTable tbl, T entity) where T : ITableEntity, new();

    Task<T> GetEntity<T>(string tableName, string partitionKey, string? rowKey = null) where T : ITableEntity, new();
    Task<T> GetEntity<T>(CloudTable tbl, string partitionKey, string? rowKey = null) where T : ITableEntity, new();

    Task<List<T>> GetEntities<T>(string tableName) where T : ITableEntity, new();
    Task<List<T>> GetEntities<T>(CloudTable tbl) where T : ITableEntity, new();
    Task<List<T>> GetEntities<T>(string tableName, string partitionKey) where T : ITableEntity, new();
    Task<List<T>> GetEntities<T>(CloudTable tbl, string partitionKey) where T : ITableEntity, new();
    Task<List<T>> GetEntities<T>(string tableName, string partitionKey, string rowKeyFrom, string rowKeyTo) where T : ITableEntity, new();
    Task<List<T>> GetEntities<T>(CloudTable tbl, string partitionKey, string rowKeyFrom, string rowKeyTo) where T : ITableEntity, new();
}

public abstract class AzTableStorageBase : IAzTableStorageBase
{
    protected readonly ILogger _logger;

    public event EventHandler<AzTableStorageArgs>? BatchCompletedEvent;
    protected virtual void OnRaiseBatchCompletedEvent(AzTableStorageArgs args) { BatchCompletedEvent?.Invoke(this, args); }

    readonly string _connectionString;

    readonly CloudStorageAccount _storageAccount;
    protected CloudTableClient _tableClient { get; set; }

    public AzTableStorageBase(ILogger<AzTableStorageBase> logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString ?? throw new ArgumentException("not supplied!", nameof(connectionString));

        _storageAccount = CloudStorageAccount.Parse(_connectionString);
        //var tableServicePoint = ServicePointManager.FindServicePoint(_storageAccount.TableEndpoint);
        //tableServicePoint.Expect100Continue = false;
        //tableServicePoint.UseNagleAlgorithm = false;
        // Create the table client.
        _tableClient = _storageAccount.CreateCloudTableClient();
    }

    public List<CloudTable> GetTables()
    {
        var tables = _tableClient.ListTables().ToList();
        return tables;
    }

    public async Task<CloudTable> GetTblRef(string tableName, bool CreateIfNotExists = true)
    {
        var tbl = _tableClient.GetTableReference(tableName);
        if (!await tbl.ExistsAsync() && CreateIfNotExists)
            await tbl.CreateIfNotExistsAsync();
        return tbl;
    }

    protected async Task<CloudTable> SetActiveTable(string tableName, bool CreateIfNotExists = true)
    {
        if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName), "expected!");
        var tbl = _tableClient.GetTableReference(tableName);
        if (!await tbl.ExistsAsync() && CreateIfNotExists) await tbl.CreateIfNotExistsAsync();
        return tbl;
    }

    #region C - Create
    //upsert single record
    public async Task<T> UpsertEntity<T>(string tableName, T entity) where T : ITableEntity, new()
    {
        var tbl = await GetTblRef(tableName);
        return await UpsertEntity<T>(tbl, entity);
    }
    public async Task<T> UpsertEntity<T>(CloudTable tbl, T entity) where T : ITableEntity, new()
    {
        var operation = TableOperation.InsertOrReplace(entity);
        var result = await tbl.ExecuteAsync(operation);
        return (T)result.Result;
    }

    //upsert batch
    public async Task<List<T>> UploadData<T>(string tableName, List<T> entities, bool verbose = false, bool useParallelism = true) where T : ITableEntity
    {
        var tbl = await GetTblRef(tableName);
        return await BatchOperation(tbl, entities, verbose, useParallelism);
    }

    public Task<List<T>> UploadData<T>(CloudTable tbl, List<T> entities, bool verbose = false, bool useParallelism = true) where T : ITableEntity => BatchOperation(tbl, entities, verbose, useParallelism);
    #endregion

    async Task<List<T>> BatchOperation<T>(CloudTable tbl, List<T> entities, bool verbose = false, bool useParallelism = true, bool InsertOrReplace = true) where T : ITableEntity
    {
        var partitions = entities.GroupBy(l => l.PartitionKey)
            .Select(g => new
            {
                PartitionKey = g.Key,
                Entities = g.Select(p => p).ToList()
            }).ToList();

        var retval = new List<T>(entities.Count);
        if (useParallelism)
            await partitions.ForEachAsyncSemaphore(p => RunBatches(p.PartitionKey, p.Entities));
        else
            await partitions.ForEachAsync(p => RunBatches(p.PartitionKey, p.Entities));
        return retval;

        async Task RunBatches(string _partitionKey, List<T> _entities)
        {
            //var batches = _entities.GetBatches(100);
            while (_entities.Count > 0)
            {
                //count how many remaining records
                var count = _entities.Count;
                var batchSize = Math.Min(100, count);//TableConstants.TableServiceBatchMaximumOperations not public in current .NET Standard implementation
                var top100 = _entities.Take(batchSize).ToList();
                var _retval = await ExecuteBatchOperation(top100);
                if (_retval.Count > 0)
                {
                    _entities.RemoveRange(0, batchSize);
                    retval!.AddRange(_retval);
                    _logger.LogDebug("account {storageAccountName}, table {tableName}, partition {partition}, (1 of {partitionCount}), {entityCount} entities handled, {remainingCount} entities remaining",
                        _storageAccount.Credentials.AccountName, tbl.Name, _partitionKey, partitions.Count, _retval.Count, count - batchSize);
                    OnRaiseBatchCompletedEvent(new AzTableStorageArgs(_storageAccount.Credentials.AccountName, tbl.Name, _partitionKey, _retval.Count, count - batchSize));
                }
                else
                {
                    _logger.LogWarning("Exiting early due to data issue...");
                    return;
                }
            }
        }

        async Task<List<T>> ExecuteBatchOperation(List<T> entityRows)
        {
            if (tbl is null) throw new ArgumentNullException(nameof(tbl));
            if (entityRows.IsNullOrEmpty()) throw new ArgumentNullException(nameof(entityRows));
            var retval = new List<T>();
            var batchOperation = new TableBatchOperation();
            foreach (var e in entityRows)
            {
                if (e != null)//just for nullable reference types...
                {
                    if (InsertOrReplace)
                    {
                        //batchOperation.Insert((ITableEntity)e);//we *want* it to error and so help identify issues with the import process!
                        batchOperation.InsertOrReplace((ITableEntity)e);
                        //batchOperation.InsertOrMerge
                    }
                    else
                        batchOperation.Delete((ITableEntity)e);
                }
            }
            try
            {
                var tableResult = await tbl.ExecuteBatchAsync(batchOperation);
                retval = tableResult.Select(p => (T)p.Result).ToList();
            }
            catch (StorageException se)
            {
                if (!se.RequestInformation.ExtendedErrorInformation.ErrorCode.Equals("ResourceNotFound"))
                {
                    _logger.LogError(se);
                    throw;
                }
                else
                {
                    //occasionally cached data might be out of sync, so ignore these errors
                    _logger.LogWarning("{exception}", se);
                }
            }
            catch (Exception ex)
            {
                //lightstreamer sometimes(?) sends the last tick upon a fresh log-in
                //which often causes a conflict with a tick already inserted *if* the application was recently rebooted!
                _logger.LogError(ex, "tableName={tableName}, entities.Count={count}", tbl.Name, entityRows.Count);
                throw;
                //FuncEmailnu.SendMessageAsync($"{Utilz.GetCallingMethodName()} TableBatchOperation failed", msg);
            }
            return retval;
        }
    }

    #region R - Read
    //single record
    public async Task<T> GetEntity<T>(string tableName, string partitionKey, string? rowKey = null) where T : ITableEntity, new()
    {
        var tbl = await GetTblRef(tableName);
        return await GetEntity<T>(tbl, partitionKey, rowKey);
    }
    public async Task<T> GetEntity<T>(CloudTable tbl, string partitionKey, string? rowKey = null) where T : ITableEntity, new()
    {
        if (string.IsNullOrWhiteSpace(rowKey))
        {
            //https://docs.microsoft.com/en-us/rest/api/storageservices/querying-tables-and-entities#sample-query-expressions
            //https://docs.microsoft.com/en-us/rest/api/storageservices/table-service-rest-api
            //https://docs.microsoft.com/en-us/rest/api/storageservices/query-entities
            //https://docs.microsoft.com/en-gb/azure/cosmos-db/table-storage-design-guide#log-tail-pattern
            //i.e. "https://myaccount.table.core.windows.net/EmployeeExpense(PartitionKey='empid')?$top=10";
            var top = 1;
            var query = $"https://{_tableClient.Credentials.AccountName}.table.core.windows.net/{tbl.Name}(PartitionKey='{partitionKey}')?$top={top}";
            throw new NotImplementedException("I need to create a REST query library for TOP 1 operations...");
        }
        var operation = TableOperation.Retrieve<T>(partitionKey, rowKey);
        var result = await tbl.ExecuteAsync(operation);
        return (T)result.Result;
    }

    //get everything
    public async Task<List<T>> GetEntities<T>(string tableName) where T : ITableEntity, new()
    {
        var tbl = await GetTblRef(tableName);
        return await GetEntities<T>(tbl);
    }
    public async Task<List<T>> GetEntities<T>(CloudTable tbl) where T : ITableEntity, new()
    {
        var entities = await tbl.ExecuteQueryAsync(new TableQuery<T>());
        return entities.ToList();
    }

    //get just the partition
    public async Task<List<T>> GetEntities<T>(string tableName, string partitionKey) where T : ITableEntity, new()
    {
        var tbl = await GetTblRef(tableName);
        return await GetEntities<T>(tbl, partitionKey);
    }
    public async Task<List<T>> GetEntities<T>(CloudTable tbl, string partitionKey) where T : ITableEntity, new()
    {
        var query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
        var entities = await tbl.ExecuteQueryAsync(query);
        return entities.ToList();
    }

    //get just the partition AND newer rowkeys within that partition
    public async Task<List<T>> GetEntities<T>(string tableName, string partitionKey, string rowKeyFrom) where T : ITableEntity, new()
    {
        var tbl = await GetTblRef(tableName);
        return await GetEntities<T>(tbl, partitionKey, rowKeyFrom);
    }
    public async Task<List<T>> GetEntities<T>(CloudTable tbl, string partitionKey, string rowKeyFrom) where T : ITableEntity, new()
    {
        var query = new TableQuery<T>().Where(TableQuery.CombineFilters(
            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
            TableOperators.And,
            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, rowKeyFrom)));
        var entities = await tbl.ExecuteQueryAsync(query);
        return entities.ToList();
    }

    //get a range within a partition
    public async Task<List<T>> GetEntities<T>(string tableName, string partitionKey, string rowKeyFrom, string rowKeyTo) where T : ITableEntity, new()
    {
        var tbl = await GetTblRef(tableName);
        return await GetEntities<T>(tbl, partitionKey, rowKeyFrom, rowKeyTo);
    }
    public async Task<List<T>> GetEntities<T>(CloudTable tbl, string partitionKey, string rowKeyFrom, string rowKeyTo) where T : ITableEntity, new()
    {
        var filterDates = TableQuery.CombineFilters(
             TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, rowKeyFrom),//add OrEqual to be inclusive
             TableOperators.And,
             TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, rowKeyTo));
        var filter = TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey), TableOperators.And, filterDates);
        var query = new TableQuery<T>().Where(filter);
        var entities = await tbl.ExecuteQueryAsync(query);
        return entities.ToList();
    }
    #endregion

    #region U - Update
    #endregion

    #region D - Delete
    public async Task DeleteTable(string tableName)
    {
        var tbl = await GetTblRef(tableName);
        if (tbl != null)
        {
            var deleted = await tbl.DeleteIfExistsAsync();
            _logger.LogDebug(deleted ? "{tableName} table deleted!" : "{tableName} table not found???", tbl.Name);
        }
    }

    public async Task DeleteData<T>(string tableName, List<T> entities) where T : ITableEntity, new()
    {
        var tbl = await GetTblRef(tableName);
        await BatchOperation(tbl, entities, useParallelism: true, InsertOrReplace: false);
    }

    public Task DeleteData<T>(CloudTable tbl, List<T> entities) where T : ITableEntity, new() => BatchOperation(tbl, entities, InsertOrReplace: false);
    #endregion
}