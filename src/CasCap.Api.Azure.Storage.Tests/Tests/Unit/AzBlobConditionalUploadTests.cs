using Azure;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Net;

namespace CasCap.Tests.Unit;

/// <summary>Exercises create-only uploads and metadata reads through the SDK pipeline with synthetic HTTP responses.</summary>
[Trait("Category", "Blob Conditional Upload")]
public sealed class AzBlobConditionalUploadTests
{
    [Fact]
    public async Task UploadBlobIfNotExistsAsync()
    {
        using var handler = new UploadHandler();
        using var http = new HttpClient(handler);
        using var content = new MemoryStream([1, 2, 3]);
        var args = new AzBlobUploadArgs
        {
            AccessTier = AccessTier.Cool,
            ContentType = "image/jpeg",
            Metadata = new Dictionary<string, string> { ["sha256"] = "abc123" },
        };

        var created = await Create(http).UploadBlobIfNotExistsAsync("photos/example.jpg", content, args,
            TestContext.Current.CancellationToken);

        Assert.True(created);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("*", request.Header("If-None-Match"));
        Assert.Equal("Cool", request.Header("x-ms-access-tier"));
        Assert.Equal("image/jpeg", request.Header("x-ms-blob-content-type"));
        Assert.Equal("abc123", request.Header("x-ms-meta-sha256"));
    }

    [Theory]
    [InlineData(409, "BlobAlreadyExists")]
    [InlineData(412, "ConditionNotMet")]
    public async Task UploadBlobIfNotExistsAsync_Exists(int status, string errorCode)
    {
        using var handler = new UploadHandler { UploadStatus = status, UploadErrorCode = errorCode };
        using var http = new HttpClient(handler);
        using var content = new MemoryStream([1]);

        Assert.False(await Create(http).UploadBlobIfNotExistsAsync("example.jpg", content,
            cancellationToken: TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(404, "ContainerNotFound")]
    [InlineData(403, "AuthorizationFailure")]
    public async Task UploadBlobIfNotExistsAsync_FailurePropagates(int status, string errorCode)
    {
        using var handler = new UploadHandler { UploadStatus = status, UploadErrorCode = errorCode };
        using var http = new HttpClient(handler);
        using var content = new MemoryStream([1]);

        var exception = await Assert.ThrowsAsync<RequestFailedException>(() => Create(http).UploadBlobIfNotExistsAsync(
            "example.jpg", content, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(status, exception.Status);
    }

    [Fact]
    public async Task GetBlobMetadataAsync()
    {
        using var handler = new UploadHandler { MetadataHash = "abc123" };
        using var http = new HttpClient(handler);

        var metadata = await Create(http).GetBlobMetadataAsync("example.jpg", TestContext.Current.CancellationToken);

        Assert.NotNull(metadata);
        Assert.Equal("abc123", metadata["sha256"]);
        Assert.Equal(HttpMethod.Head, Assert.Single(handler.Requests).Method);
    }

    [Fact]
    public async Task GetBlobMetadataAsync_Missing()
    {
        using var handler = new UploadHandler();
        using var http = new HttpClient(handler);

        Assert.Null(await Create(http).GetBlobMetadataAsync("example.jpg", TestContext.Current.CancellationToken));
    }

    private static TestStorage Create(HttpClient http) => new(new BlobContainerClient(
        new Uri("https://storage.example.com/media"), new BlobClientOptions
        {
            Transport = new HttpClientTransport(http),
            Retry = { MaxRetries = 0 },
        }));

    private sealed class TestStorage(BlobContainerClient containerClient) : AzBlobStorageBase(containerClient);

    private sealed record RecordedRequest(HttpMethod Method, Dictionary<string, string> Headers)
    {
        public string? Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;
    }

    private sealed class UploadHandler : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];
        public int UploadStatus { get; init; } = 201;
        public string? UploadErrorCode { get; init; }
        public string? MetadataHash { get; init; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var headers = request.Headers.Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
                .ToDictionary(header => header.Key, header => string.Join(",", header.Value), StringComparer.OrdinalIgnoreCase);
            Requests.Add(new RecordedRequest(request.Method, headers));

            var response = request.Method == HttpMethod.Head
                ? new HttpResponseMessage(MetadataHash is null ? HttpStatusCode.NotFound : HttpStatusCode.OK)
                : new HttpResponseMessage((HttpStatusCode)UploadStatus);
            response.Content = new ByteArrayContent([]);
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"example-revision\"");
            response.Content.Headers.LastModified = DateTimeOffset.UnixEpoch;
            if (request.Method == HttpMethod.Head && MetadataHash is not null)
            {
                response.Headers.Add("x-ms-blob-type", "BlockBlob");
                response.Headers.Add("x-ms-meta-sha256", MetadataHash);
            }
            if (request.Method != HttpMethod.Head && UploadErrorCode is not null)
                response.Headers.Add("x-ms-error-code", UploadErrorCode);
            return Task.FromResult(response);
        }
    }
}
