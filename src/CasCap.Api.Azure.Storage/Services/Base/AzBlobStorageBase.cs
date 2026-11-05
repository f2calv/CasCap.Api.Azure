namespace CasCap.Services;

/// <inheritdoc/>
public abstract class AzBlobStorageBase : IAzBlobStorageBase
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(AzBlobStorageBase));

    private readonly BlobContainerClient _containerClient;

    /// <inheritdoc/>
    public string ContainerName { get; private set; }

    /// <summary>Uses an existing client without network I/O or provisioning; client configuration remains caller-owned.</summary>
    protected AzBlobStorageBase(BlobContainerClient containerClient)
    {
        ArgumentNullException.ThrowIfNull(containerClient);
        _containerClient = containerClient;
        ContainerName = containerClient.Name;
    }

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
        var response = await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return response is not null;
    }

    /// <inheritdoc/>
    public async Task DeleteBlob(string blobName, CancellationToken cancellationToken)
        => _ = await _containerClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<byte[]?> DownloadBlobAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("{ClassName} blob does not exist in container {ContainerName}", nameof(AzBlobStorageBase), _containerClient.Name);
            return null;
        }
        var response = await blobClient.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
        return response.Value.Content.ToArray();
    }

    /// <inheritdoc/>
    public Task<Stream> OpenBlobReadAsync(string blobName, ETag ifMatch, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        var tag = ifMatch.ToString();
        ArgumentException.ThrowIfNullOrWhiteSpace(tag, nameof(ifMatch));
        if (tag == "*" || tag.StartsWith("W/", StringComparison.OrdinalIgnoreCase)
            || tag.Contains('\r') || tag.Contains('\n') || tag.Contains(','))
            throw new ArgumentException("A single exact, strong entity tag is required.", nameof(ifMatch));
        cancellationToken.ThrowIfCancellationRequested();
        return _containerClient.GetBlobClient(blobName).OpenReadAsync(
            new BlobOpenReadOptions(allowModifications: false)
            {
                BufferSize = 65_536,
                Conditions = new BlobRequestConditions { IfMatch = ifMatch },
            }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<BlobItem>> ListContainerBlobs(string? prefix = null, CancellationToken cancellationToken = default)
    {
        var blobs = new List<BlobItem>();
        await foreach (var blobItem in _containerClient.GetBlobsAsync(new GetBlobsOptions { Prefix = prefix }, cancellationToken: cancellationToken).ConfigureAwait(false))
            blobs.Add(blobItem);
        return blobs;
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetBlobPrefixes(string? prefix = null, CancellationToken cancellationToken = default)
    {
        var hs = new HashSet<string>();
        await foreach (var hierarchyItem in _containerClient.GetBlobsByHierarchyAsync(new GetBlobsByHierarchyOptions { Prefix = prefix, Delimiter = "/" }, cancellationToken: cancellationToken).ConfigureAwait(false))
            hs.Add(hierarchyItem.Prefix);
        var prefixes = hs.Select(p => p.Replace("/", string.Empty)).ToList();
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("{ClassName} blob prefixes returned PrefixCount={PrefixCount}",
                nameof(AzBlobStorageBase), prefixes.Count);
        return prefixes;
    }

    /// <inheritdoc/>
    public async Task UploadBlob(string blobName, byte[] bytes, CancellationToken cancellationToken)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        using var stream = new MemoryStream(bytes, writable: false);
        _ = await blobClient.UploadAsync(stream, true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UploadBlob(string blobName, Stream stream, CancellationToken cancellationToken)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        _ = await blobClient.UploadAsync(stream, true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> UploadBlobIfNotExistsAsync(string blobName, Stream stream, AzBlobUploadArgs? args = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentNullException.ThrowIfNull(stream);
        var options = new BlobUploadOptions
        {
            AccessTier = args?.AccessTier,
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
            HttpHeaders = args?.ContentType is { } contentType ? new BlobHttpHeaders { ContentType = contentType } : null,
            Metadata = args?.Metadata is { } metadata ? new Dictionary<string, string>(metadata) : null,
        };
        try
        {
            _ = await _containerClient.GetBlobClient(blobName).UploadAsync(stream, options, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobAlreadyExists || ex.ErrorCode == BlobErrorCode.ConditionNotMet)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IDictionary<string, string>?> GetBlobMetadataAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        try
        {
            var properties = await _containerClient.GetBlobClient(blobName).GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return properties.Value.Metadata;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
