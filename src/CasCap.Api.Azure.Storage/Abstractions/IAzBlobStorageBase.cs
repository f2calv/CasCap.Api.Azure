namespace CasCap.Abstractions;

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
