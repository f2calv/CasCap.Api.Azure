﻿namespace CasCap.Services;

public interface IAzBlobStorageBase
{
    Task<bool> CreateContainerIfNotExists(string containerName, CancellationToken cancellationToken);
    //Task DeleteBlob(CloudBlobContainer container, string blobName);
    Task DeleteBlob(string containerName, string blobName, CancellationToken cancellationToken);
    Task<byte[]?> DownloadBlobAsync(string blobName, string? containerName = null, CancellationToken cancellationToken = default);
    Task<List<BlobItem>> ListContainerBlobs(string? containerName = null, string? prefix = null, CancellationToken cancellationToken = default);
    Task<List<string>> GetBlobPrefixes(string? containerName = null, string? prefix = null, CancellationToken cancellationToken = default);
    //Task<List<CloudPageBlob>> ListContainerPageBlobs(string? containerName = null, string? prefix = null);
    //Task<List<string>> ListContainers();
    Task UploadBlob(string blobName, byte[] bytes, CancellationToken cancellationToken);
    Task PageBlobTest(CancellationToken cancellationToken = default);
}

public abstract class AzBlobStorageBase : IAzBlobStorageBase
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(AzBlobStorageBase));
    private readonly string _connectionString;
    private readonly string _containerName;

    private readonly BlobContainerClient _containerClient;

    public AzBlobStorageBase(string connectionString, string containerName)
    {
        _connectionString = connectionString ?? throw new ArgumentException("not supplied!", nameof(connectionString));
        _containerName = containerName ?? throw new ArgumentException("not supplied!", nameof(containerName));

        if (string.IsNullOrWhiteSpace(_connectionString) || string.IsNullOrWhiteSpace(_containerName))
            throw new ArgumentException("connectionString and/or _queueName not set!");

        _containerClient = new BlobContainerClient(_connectionString, _containerName);
    }

    //https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet
    //https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-pageblob-overview?tabs=dotnet
    public async Task PageBlobTest(CancellationToken cancellationToken = default)
    {
        //https://docs.microsoft.com/en-us/rest/api/storageservices/understanding-block-blobs--append-blobs--and-page-blobs?WT.mc_id=AZ-MVP-5003203

        //First, get a reference to a container. To create a page blob, call the GetPageBlobClient method, and then call the PageBlobClient.Create method.
        //Pass in the max size for the blob to create. That size must be a multiple of 512 bytes.

        long OneGigabyteAsBytes = 1024 * 1024 * 1024;

        //BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);
        //var blobContainerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        //var pageBlobClient = blobContainerClient.GetPageBlobClient("0s4.vhd");
        //pageBlobClient.Create(16 * OneGigabyteAsBytes);

        var pageBlobClient = _containerClient.GetPageBlobClient("test.bin");
        pageBlobClient.Create(1 * OneGigabyteAsBytes, cancellationToken: cancellationToken);

        //Resizing a page blob
        //pageBlobClient.Resize(32 * OneGigabyteAsBytes, cancellationToken: cancellationToken);

        //Writing pages to a page blob
        //var array = new byte[512];

        var bytes = await File.ReadAllBytesAsync("c:/temp/test.zip", cancellationToken);//200kb

        if (bytes.Length > 4 * 1024 * 1024)
            throw new Exception("bigger than 4mb");

        using (var stream = new MemoryStream(bytes))
        {
            var res = pageBlobClient.UploadPages(stream, 0, cancellationToken: cancellationToken);
            //Debugger.Break();
            //await blockBlob.UploadFromStreamAsync(stream);
        }

        IEnumerable<HttpRange> pageRanges = pageBlobClient.GetPageRanges(cancellationToken: cancellationToken).Value.PageRanges;

        foreach (var range in pageRanges)
        {
            _ = pageBlobClient.Download(range, cancellationToken: cancellationToken);
        }

        _ = pageBlobClient.Download(new HttpRange(0, 1000), cancellationToken: cancellationToken);
        //Debugger.Break();
    }

    public async Task<bool> CreateContainerIfNotExists(string containerName, CancellationToken cancellationToken)
    {
        if (!await _containerClient.ExistsAsync(cancellationToken))
        {
            _ = await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            return true;
        }
        else
            return false;
    }

    public async Task DeleteBlob(string containerName, string blobName, CancellationToken cancellationToken)
    {
        var _containerClient = new BlobContainerClient(_connectionString, containerName);
        _ = await _containerClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
    }

    public async Task<byte[]?> DownloadBlobAsync(string blobName, string? containerName = null, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient;
        byte[] bytes;
        if (!string.IsNullOrWhiteSpace(containerName))
        {
            var _containerClient = new BlobContainerClient(_connectionString, containerName);
            blobClient = _containerClient.GetBlobClient(blobName);
        }
        else
            blobClient = _containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("{className} blob {blobName} does not exist", nameof(AzBlobStorageBase), blobName);
            return null;
        }
        var downloadInfo = await blobClient.DownloadAsync(cancellationToken);
        using (var ms = new MemoryStream())
        {
            await downloadInfo.Value.Content.CopyToAsync(ms, cancellationToken);
            bytes = ms.ToArray();
        }
        return bytes;
    }

    public async Task<List<BlobItem>> ListContainerBlobs(string? containerName = null, string? prefix = null, CancellationToken cancellationToken = default)
    {
        var l = new List<BlobItem>();
        if (!string.IsNullOrWhiteSpace(containerName))
        {
            var _containerClient = new BlobContainerClient(_connectionString, containerName);
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
                l.Add(blobItem);
        }
        else
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
                l.Add(blobItem);
        return l;
    }

    public async Task<List<BlobItem>> ListBlobs(string? containerName = null, string? prefix = null, CancellationToken cancellationToken = default)
    {
        var l = new List<BlobItem>();
        if (!string.IsNullOrWhiteSpace(containerName))
        {
            var _containerClient = new BlobContainerClient(_connectionString, containerName);
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
                l.Add(blobItem);
        }
        else
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
                l.Add(blobItem);

        return l;
    }

    public async Task<List<string>> GetBlobPrefixes(string? containerName = null, string? prefix = null, CancellationToken cancellationToken = default)
    {
        var hs = new HashSet<string>();
        if (!string.IsNullOrWhiteSpace(containerName))
        {
            var _containerClient = new BlobContainerClient(_connectionString, containerName);
            await foreach (var hierarchyItem in _containerClient.GetBlobsByHierarchyAsync(prefix: prefix, delimiter: "/", cancellationToken: cancellationToken))
                hs.Add(hierarchyItem.Prefix);
        }
        else
            await foreach (var hierarchyItem in _containerClient.GetBlobsByHierarchyAsync(prefix: prefix, delimiter: "/", cancellationToken: cancellationToken))
                hs.Add(hierarchyItem.Prefix);
        var prefixes = hs.Select(p => p.Replace("/", string.Empty)).ToList();
        _logger.LogInformation("{className} Symbols/prefixes return from blob storage are; {symbols}",
            nameof(AzBlobStorageBase), prefixes);
        return prefixes;
    }

    public async Task UploadBlob(string blobName, byte[] bytes, CancellationToken cancellationToken = default)
    {
        var _blobClient = _containerClient.GetBlobClient(blobName);
        using var stream = new MemoryStream(bytes, writable: false);
        var res = await _blobClient.UploadAsync(stream, true, cancellationToken);
    }
}
