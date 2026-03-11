namespace CasCap.Abstractions;

/// <summary>Base abstraction for Azure Blob Storage container operations.</summary>
public interface IAzBlobStorageBase
{
    /// <summary>Gets the name of the blob container.</summary>
    string ContainerName { get; }

    /// <summary>Creates the container if it does not already exist.</summary>
    Task<bool> CreateContainerIfNotExists(CancellationToken cancellationToken);

    /// <summary>Deletes the named blob from the container.</summary>
    Task DeleteBlob(string blobName, CancellationToken cancellationToken);

    /// <summary>Downloads the named blob and returns its contents as a byte array, or <see langword="null" /> if the blob does not exist.</summary>
    Task<byte[]?> DownloadBlobAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>Lists all blobs in the container, optionally filtered by <paramref name="prefix"/>.</summary>
    Task<List<BlobItem>> ListContainerBlobs(string? prefix = null, CancellationToken cancellationToken = default);

    /// <summary>Returns the set of virtual-directory prefixes (folders) present in the container.</summary>
    Task<List<string>> GetBlobPrefixes(string? prefix = null, CancellationToken cancellationToken = default);

    /// <summary>Uploads a byte array as a blob with the given name, overwriting any existing blob.</summary>
    Task UploadBlob(string blobName, byte[] bytes, CancellationToken cancellationToken);

    /// <summary>Runs a page-blob write/read round-trip test using the file at <paramref name="path"/>.</summary>
    Task PageBlobTest(string path, CancellationToken cancellationToken = default);
}
