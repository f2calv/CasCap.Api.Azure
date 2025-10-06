namespace CasCap.Abstractions;

public interface IAzBlobStorageBase
{
    string ContainerName { get; }
    Task<bool> CreateContainerIfNotExists(CancellationToken cancellationToken);
    //Task DeleteBlob(CloudBlobContainer container, string blobName);
    Task DeleteBlob(string blobName, CancellationToken cancellationToken);
    Task<byte[]?> DownloadBlobAsync(string blobName, CancellationToken cancellationToken = default);
    Task<List<BlobItem>> ListContainerBlobs(string? prefix = null, CancellationToken cancellationToken = default);
    Task<List<string>> GetBlobPrefixes(string? prefix = null, CancellationToken cancellationToken = default);
    //Task<List<CloudPageBlob>> ListContainerPageBlobs(string? prefix = null);
    //Task<List<string>> ListContainers();
    Task UploadBlob(string blobName, byte[] bytes, CancellationToken cancellationToken);
    Task PageBlobTest(string path, CancellationToken cancellationToken = default);
}
