using CasCap.Common.Extensions;
using CasCap.Common.Logging;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace CasCap.Services;

[Obsolete]
public abstract class AzBlobStorageBaseOLD : IAzBlobStorageBase
{
    readonly ILogger _logger = ApplicationLogging.CreateLogger<AzBlobStorageBaseOLD>();

    //public event EventHandler<AzQueueStorageArgs> BatchCompletedEvent;
    //protected virtual void OnRaiseBatchCompletedEvent(AzQueueStorageArgs args) { BatchCompletedEvent?.Invoke(this, args); }

    readonly string _connectionString;
    readonly string _containerName;

    readonly CloudStorageAccount _storageAccount;
    readonly CloudBlobClient _blobClient;
    protected CloudBlobContainer _container { get; set; }

    public AzBlobStorageBaseOLD(string connectionString, string containerName)
    {
        _connectionString = connectionString ?? throw new ArgumentException("not supplied!", nameof(connectionString));
        _containerName = containerName ?? throw new ArgumentException("not supplied!", nameof(containerName));

        if (string.IsNullOrWhiteSpace(_connectionString) || string.IsNullOrWhiteSpace(_containerName))
            throw new ArgumentException("connectionString and/or _queueName not set!");

        _storageAccount = CloudStorageAccount.Parse(_connectionString);
        _blobClient = _storageAccount.CreateCloudBlobClient();
        _container = _blobClient.GetContainerReference(containerName);
    }

    async Task<List<string>> ListContainers()
    {
        var containers = await _blobClient.ListContainersAsync();
        foreach (var _container in containers)
        {
            //_logger.LogDebug(_container.Name);
        }
        return containers.Select(p => p.Name).ToList();
    }

    enum blobType
    {
        CloudBlockBlob,
        CloudPageBlob,
        CloudBlobDirectory
    }

    public async Task<bool> CreateContainerIfNotExists(string containerName)
    {
        await Task.Delay(0);
        if (!string.IsNullOrWhiteSpace(containerName))
            _container = _blobClient.GetContainerReference(containerName);
        return await _container.CreateIfNotExistsAsync();
    }

    public async Task<List<Azure.Storage.Blobs.Models.BlobItem>> ListContainerBlobs(string? containerName = null, string? prefix = null)
    {
        await Task.Delay(0);
        throw new NotSupportedException("deprecated!");
        //var lst = await ListContainerContents(blobType.CloudBlockBlob, containerName, prefix);
        //return lst.Select(p => (CloudBlockBlob)p).ToList();
    }

    //public async Task<List<CloudPageBlob>> ListContainerPageBlobs(string? containerName = null, string? prefix = null)
    //{
    //    var lst = await ListContainerContents(blobType.CloudPageBlob, containerName, prefix);
    //    return lst.Select(p => (CloudPageBlob)p).ToList();
    //}

    public async Task<List<string>> GetBlobPrefixes(string? containerName = null, string? prefix = null)
    {
        var lst = await ListContainerContents(blobType.CloudBlobDirectory, containerName, prefix);
        var l = lst.Select(p => (CloudBlobDirectory)p).ToList();
        var prefixes = l.Select(p => p.Prefix.Replace("/", string.Empty)).ToList();
        return prefixes;
    }

    async Task<List<IListBlobItem>> ListContainerContents(blobType type, string? containerName = null, string? prefix = null)
    {
        if (!string.IsNullOrWhiteSpace(containerName))
            _container = _blobClient.GetContainerReference(containerName);
        var output = new List<IListBlobItem>();
        // Loop over items within the container and output the length and URI.
        var lst = await _container.ListBlobsAsync(prefix!, false);
        foreach (var item in lst)
        {
            if (type == blobType.CloudBlockBlob && item.GetType() == typeof(CloudBlockBlob))
                output.Add(item);
            else if (type == blobType.CloudPageBlob && item.GetType() == typeof(CloudPageBlob))
                output.Add(item);
            else if (type == blobType.CloudBlobDirectory && item.GetType() == typeof(CloudBlobDirectory))
                output.Add(item);
            else
                throw new NotImplementedException("erm?");
        }
        return output;
    }

    #region Upload
    public async Task UploadBlob(string blobName, byte[] bytes)
    {
        var blockBlob = _container.GetBlockBlobReference(blobName);
        using var stream = new MemoryStream(bytes, writable: false);
        await blockBlob.UploadFromStreamAsync(stream);
    }
    //public void Upload(string blobName, string strInput)
    //{
    //    var bytes = Encoding.ASCII.GetBytes(strInput);
    //    UploadAsync(blobName, bytes);
    //}
    #endregion

    #region Delete
    public async Task DeleteBlob(string containerName, string blobName)
    {
        var container = _blobClient.GetContainerReference(containerName);
        await DeleteBlob(container, blobName);
    }

    async Task DeleteBlob(CloudBlobContainer container, string blobName)
    {
        if (await container.ExistsAsync())
        {
            var blockBlob = container.GetBlockBlobReference(blobName);
            if (await blockBlob.ExistsAsync())
                await blockBlob.DeleteAsync();
        }
    }

    async Task DeleteBlob(CloudPageBlob blob)
    {
        if (await blob.ExistsAsync())
            await blob.DeleteAsync();
    }
    #endregion

    #region Download
    public Task<byte[]?> DownloadBlobAsync(string blobName, string? containerName = null)
    {
        return DownloadBytesAsync(blobType.CloudBlockBlob, blobName, containerName);
    }

    async Task<byte[]?> DownloadBytesAsync(blobType type, string blobName, string? containerName = null)
    {
        byte[]? bytes = null;
        if (!string.IsNullOrWhiteSpace(containerName)) _container = _blobClient.GetContainerReference(containerName);
        if (type == blobType.CloudBlockBlob)
        {
            var blob = _container.GetBlockBlobReference(blobName);
            if (await blob.ExistsAsync())
            {
                await blob.FetchAttributesAsync();
                bytes = new byte[blob.Properties.Length];
                await blob.DownloadToByteArrayAsync(bytes, 0);
            }
        }
        else if (type == blobType.CloudPageBlob)
        {
            var blob = _container.GetPageBlobReference(blobName);
            if (await blob.ExistsAsync())
            {
                await blob.FetchAttributesAsync();
                bytes = new byte[blob.Properties.Length];
                await blob.DownloadToByteArrayAsync(bytes, 0);
            }
        }
        else
            throw new NotImplementedException("erm?");
        return bytes;
    }
    #endregion
}