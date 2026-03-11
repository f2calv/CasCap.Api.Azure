namespace CasCap.Abstractions;

/// <summary>
/// Defines the contract for interacting with an Azure Blob Storage container.
/// </summary>
public interface IAzBlobStorageBase
{
    /// <summary>Gets the name of the blob container this instance targets.</summary>
    string ContainerName { get; }

    /// <summary>
    /// Creates the blob container if it does not already exist.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns><see langword="true"/> if the container was created; <see langword="false"/> if it already existed.</returns>
    Task<bool> CreateContainerIfNotExists(CancellationToken cancellationToken);

    //Task DeleteBlob(CloudBlobContainer container, string blobName);
    /// <summary>
    /// Deletes the blob with the specified name from the container, if it exists.
    /// </summary>
    /// <param name="blobName">The name of the blob to delete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    Task DeleteBlob(string blobName, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads the content of a blob as a byte array.
    /// </summary>
    /// <param name="blobName">The name of the blob to download.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The blob content as a byte array, or <see langword="null"/> if the blob does not exist.</returns>
    Task<byte[]?> DownloadBlobAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all blobs in the container, optionally filtered by a name prefix.
    /// </summary>
    /// <param name="prefix">An optional prefix used to filter the blobs returned. Pass <see langword="null"/> to list all blobs.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A list of <see cref="BlobItem"/> objects representing the blobs in the container.</returns>
    Task<List<BlobItem>> ListContainerBlobs(string? prefix = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the distinct virtual directory prefixes (one level deep) within the container.
    /// </summary>
    /// <param name="prefix">An optional prefix used to scope the hierarchy query. Pass <see langword="null"/> to start from the container root.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A list of prefix strings representing the virtual directories found in the container.</returns>
    Task<List<string>> GetBlobPrefixes(string? prefix = null, CancellationToken cancellationToken = default);

    //Task<List<CloudPageBlob>> ListContainerPageBlobs(string? prefix = null);
    //Task<List<string>> ListContainers();
    /// <summary>
    /// Uploads a byte array to the container as a blob with the specified name, overwriting any existing blob.
    /// </summary>
    /// <param name="blobName">The name to assign to the uploaded blob.</param>
    /// <param name="bytes">The byte array content to upload.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    Task UploadBlob(string blobName, byte[] bytes, CancellationToken cancellationToken);

    /// <summary>
    /// Runs a page blob write/read test by creating a 1 GB page blob and uploading the contents of the specified file.
    /// </summary>
    /// <param name="path">The local file path whose contents will be written to the page blob. Must be 4 MB or smaller.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    Task PageBlobTest(string path, CancellationToken cancellationToken = default);
}
