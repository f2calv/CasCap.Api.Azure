namespace CasCap.Services;

/// <inheritdoc/>
public abstract class AzBlobStorageBase : IAzBlobStorageBase
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(AzBlobStorageBase));

    private readonly BlobContainerClient _containerClient;

    /// <inheritdoc/>
    public string ContainerName { get; private set; }

    /// <summary>Initializes a new instance of <see cref="AzBlobStorageBase" /> using a connection string.</summary>
    protected AzBlobStorageBase(string connectionString, string containerName, BlobClientOptions.ServiceVersion? serviceVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ContainerName = containerName;
        var options = serviceVersion.HasValue ? new BlobClientOptions(serviceVersion.Value) : null;
        _containerClient = options is not null
            ? new BlobContainerClient(connectionString, containerName, options)
            : new BlobContainerClient(connectionString, containerName);
        _containerClient.CreateIfNotExists();
    }

    /// <summary>Initializes a new instance of <see cref="AzBlobStorageBase" /> using a URI and token credential.</summary>
    protected AzBlobStorageBase(Uri blobContainerUri, string containerName, TokenCredential credential, BlobClientOptions.ServiceVersion? serviceVersion = null)
    {
        ArgumentNullException.ThrowIfNull(blobContainerUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ContainerName = containerName;
        ArgumentNullException.ThrowIfNull(credential);
        var options = serviceVersion.HasValue ? new BlobClientOptions(serviceVersion.Value) : null;
        _containerClient = options is not null
            ? new BlobContainerClient(new Uri(blobContainerUri, containerName), credential, options)
            : new BlobContainerClient(new Uri(blobContainerUri, containerName), credential);
        _containerClient.CreateIfNotExists();
    }

    /// <inheritdoc/>
    public async Task<bool> CreateContainerIfNotExists(CancellationToken cancellationToken)
    {
        var response = await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        return response is not null;
    }

    /// <inheritdoc/>
    public async Task DeleteBlob(string blobName, CancellationToken cancellationToken)
        => _ = await _containerClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);

    /// <inheritdoc/>
    public async Task<byte[]?> DownloadBlobAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("{ClassName} blob {BlobName} does not exist", nameof(AzBlobStorageBase), blobName);
            return null;
        }
        var response = await blobClient.DownloadContentAsync(cancellationToken);
        return response.Value.Content.ToArray();
    }

    /// <inheritdoc/>
    public async Task<List<BlobItem>> ListContainerBlobs(string? prefix = null, CancellationToken cancellationToken = default)
    {
        var blobs = new List<BlobItem>();
        await foreach (var blobItem in _containerClient.GetBlobsAsync(new GetBlobsOptions { Prefix = prefix }, cancellationToken: cancellationToken))
            blobs.Add(blobItem);
        return blobs;
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetBlobPrefixes(string? prefix = null, CancellationToken cancellationToken = default)
    {
        var hs = new HashSet<string>();
        await foreach (var hierarchyItem in _containerClient.GetBlobsByHierarchyAsync(new GetBlobsByHierarchyOptions { Prefix = prefix, Delimiter = "/" }, cancellationToken: cancellationToken))
            hs.Add(hierarchyItem.Prefix);
        var prefixes = hs.Select(p => p.Replace("/", string.Empty)).ToList();
        _logger.LogDebug("{ClassName} prefixes returned from blob storage are; {Prefixes}",
            nameof(AzBlobStorageBase), prefixes);
        return prefixes;
    }

    /// <inheritdoc/>
    public async Task UploadBlob(string blobName, byte[] bytes, CancellationToken cancellationToken)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        using var stream = new MemoryStream(bytes, writable: false);
        _ = await blobClient.UploadAsync(stream, true, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UploadBlob(string blobName, Stream stream, CancellationToken cancellationToken)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        _ = await blobClient.UploadAsync(stream, true, cancellationToken);
    }
}
