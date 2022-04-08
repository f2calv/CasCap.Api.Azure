using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace CasCap.Apis.AzStorage.Tests;

public class AzBlobStorageTests : TestBase
{
    public AzBlobStorageTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task AzBlob()
    {
        var blobName = $"{Guid.NewGuid()}.bin";

        var containerName = "test";

        await _blobSvc.CreateContainerIfNotExists(containerName);

        await _blobSvc.UploadBlob($"{Guid.NewGuid()}.bin", fileBytes);

        await _blobSvc.UploadBlob(blobName, fileBytes);

        var downloadedBytes = await _blobSvc.DownloadBlobAsync(blobName);
        Assert.NotNull(downloadedBytes);

    }

    //[Fact]
    //public async Task RunThrough()
    //{
    //    await foreach (var container in _blobSvc.GetBlobContainersAsync())
    //        _logger.LogDebug($"{container.Name} ({container.Properties.PublicAccess})");

    //    await foreach (var container in _blobSvc.GetBlobContainersAsync(prefix: containerName))
    //        Debug.WriteLine($"{container.Name} ({container.Properties.PublicAccess})");

    //    /*
    //    //if we *know* the container already exists, generate the container client
    //    _containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
    //    // if we *know* the container doesnt exist, create the container and return a container client
    //    var test = await _blobServiceClient.CreateBlobContainerAsync(containerName);
    //    _containerClient = test.Value;
    //    */
    //    //if we *don't know* if the container already exists, generate the container client and attempt a create

    //    await foreach (var item in _containerClient.GetBlobsByHierarchyAsync())
    //    {
    //        Debug.WriteLine(item.Blob.Name);
    //    }

    //    // Get a reference to a blob
    //    var filename = $"subfolder/test{Guid.NewGuid()}.bin";
    //    var _blobClient = _containerClient.GetBlobClient(filename);

    //    Debug.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", _blobClient.Uri);

    //    // Open the file and upload its data
    //    //using FileStream uploadFileStream = File.OpenRead(localFilePath);

    //    using (var stream = new MemoryStream(fileBytes, writable: false))
    //    {
    //        var res = await _blobClient.UploadAsync(stream, true);
    //        stream.Close();
    //    }

    //    // List all blobs in the container
    //    await foreach (var blobItem in _containerClient.GetBlobsAsync())
    //    {
    //        Debug.WriteLine("\t" + blobItem.Name);
    //    }


    //    var downloadFilePath = Path.Combine("c:/temp", filename);
    //    Console.WriteLine("\nDownloading blob to\n\t{0}\n", downloadFilePath);
    //    // Download the blob's contents and save it to a file
    //    var downloadInfo = await _blobClient.DownloadAsync();

    //    var dir = Path.GetDirectoryName(downloadFilePath);
    //    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    //    using (var fs = File.OpenWrite(downloadFilePath))
    //    {
    //        await downloadInfo.Value.Content.CopyToAsync(fs);
    //        fs.Close();
    //    }
    //    Assert.True(true);
    //}
}