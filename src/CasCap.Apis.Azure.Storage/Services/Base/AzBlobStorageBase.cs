namespace CasCap.Services;

public interface IAzBlobStorageBase
{
    Task<bool> CreateContainerIfNotExists(string containerName);
    //Task DeleteBlob(CloudBlobContainer container, string blobName);
    Task DeleteBlob(string containerName, string blobName);
    Task<byte[]?> DownloadBlobAsync(string blobName, string? containerName = null);
    Task<List<BlobItem>> ListContainerBlobs(string? containerName = null, string? prefix = null);
    Task<List<string>> GetBlobPrefixes(string? containerName = null, string? prefix = null);
    //Task<List<CloudPageBlob>> ListContainerPageBlobs(string? containerName = null, string? prefix = null);
    //Task<List<string>> ListContainers();
    Task UploadBlob(string blobName, byte[] bytes);
    Task PageBlobTest();
}

public abstract class AzBlobStorageBase : IAzBlobStorageBase
{
    readonly ILogger _logger;
    readonly string _connectionString;
    readonly string _containerName;

    readonly BlobContainerClient _containerClient;

    public AzBlobStorageBase(ILogger<AzBlobStorageBase> logger, string connectionString, string containerName)
    {
        _logger = logger;
        _connectionString = connectionString ?? throw new ArgumentException("not supplied!", nameof(connectionString));
        _containerName = containerName ?? throw new ArgumentException("not supplied!", nameof(containerName));

        if (string.IsNullOrWhiteSpace(_connectionString) || string.IsNullOrWhiteSpace(_containerName))
            throw new ArgumentException("connectionString and/or _queueName not set!");

        _containerClient = new BlobContainerClient(_connectionString, _containerName);
    }

    //https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet
    //https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-pageblob-overview?tabs=dotnet
    public async Task PageBlobTest()
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
        pageBlobClient.Create(1 * OneGigabyteAsBytes);

        //Resizing a page blob
        //pageBlobClient.Resize(32 * OneGigabyteAsBytes);

        //Writing pages to a page blob
        var array = new byte[512];

        var bytes = await File.ReadAllBytesAsync("c:/temp/test.zip");//200kb

        if (bytes.Length > 4 * 1024 * 1024)
            throw new Exception("bigger than 4mb");

        using (var stream = new MemoryStream(bytes))
        {
            var res = pageBlobClient.UploadPages(stream, 0);
            //Debugger.Break();
            //await blockBlob.UploadFromStreamAsync(stream);
        }

        IEnumerable<HttpRange> pageRanges = pageBlobClient.GetPageRanges().Value.PageRanges;

        foreach (var range in pageRanges)
        {
            var pageBlob = pageBlobClient.Download(range);
        }

        var pageBlob2 = pageBlobClient.Download(new HttpRange(0, 1000));
        //Debugger.Break();
    }

    public async Task<bool> CreateContainerIfNotExists(string containerName)
    {
        if (!await _containerClient.ExistsAsync())
        {
            _ = await _containerClient.CreateIfNotExistsAsync();
            return true;
        }
        else
            return false;
    }

    public async Task DeleteBlob(string containerName, string blobName)
    {
        var _containerClient = new BlobContainerClient(_connectionString, containerName);
        _ = await _containerClient.DeleteBlobIfExistsAsync(blobName);
    }

    public async Task<byte[]?> DownloadBlobAsync(string blobName, string? containerName = null)
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

        if (!await blobClient.ExistsAsync())
        {
            _logger.LogWarning("blob {blobName} does not exist", blobName);
            return null;
        }
        var downloadInfo = await blobClient.DownloadAsync();
        using (var ms = new MemoryStream())
        {
            await downloadInfo.Value.Content.CopyToAsync(ms);
            bytes = ms.ToArray();
        }
        return bytes;
    }

    public async Task<List<BlobItem>> ListContainerBlobs(string? containerName = null, string? prefix = null)
    {
        var l = new List<BlobItem>();
        if (!string.IsNullOrWhiteSpace(containerName))
        {
            var _containerClient = new BlobContainerClient(_connectionString, containerName);
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix))
                l.Add(blobItem);
        }
        else
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix))
                l.Add(blobItem);
        return l;
    }

    public async Task<List<BlobItem>> ListBlobs(string? containerName = null, string? prefix = null)
    {
        var l = new List<BlobItem>();
        if (!string.IsNullOrWhiteSpace(containerName))
        {
            var _containerClient = new BlobContainerClient(_connectionString, containerName);
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix))
                l.Add(blobItem);
        }
        else
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix))
                l.Add(blobItem);

        return l;
    }

    public async Task<List<string>> GetBlobPrefixes(string? containerName = null, string? prefix = null)
    {
        var hs = new HashSet<string>();
        if (!string.IsNullOrWhiteSpace(containerName))
        {
            var _containerClient = new BlobContainerClient(_connectionString, containerName);
            await foreach (var hierarchyItem in _containerClient.GetBlobsByHierarchyAsync(prefix: prefix, delimiter: "/"))
                hs.Add(hierarchyItem.Prefix);
        }
        else
            await foreach (var hierarchyItem in _containerClient.GetBlobsByHierarchyAsync(prefix: prefix, delimiter: "/"))
                hs.Add(hierarchyItem.Prefix);
        var prefixes = hs.Select(p => p.Replace("/", string.Empty)).ToList();
        _logger.LogInformation("Symbols/prefixes return from blob storage are; {symbols}", prefixes);
        return prefixes;
    }

    public async Task UploadBlob(string blobName, byte[] bytes)
    {
        var _blobClient = _containerClient.GetBlobClient(blobName);
        using var stream = new MemoryStream(bytes, writable: false);
        var res = await _blobClient.UploadAsync(stream, true);
    }
}
