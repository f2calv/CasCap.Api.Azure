using Azure;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace CasCap.Tests.Unit;

/// <summary>Exercises the actual SDK pipeline using synthetic in-memory HTTP responses, never Azure or Azurite.</summary>
[Trait("Category", "Blob Conditional Read")]
public sealed class AzBlobConditionalReadTests
{
    private static readonly ETag Revision = new("\"example-revision\"");

    [Fact]
    public async Task ConditionalRangesAndOwnership()
    {
        using var handler = new BlobHandler();
        using var http = new HttpClient(handler);
        var storage = Create(http);
        Assert.Empty(handler.Conditions);
        await using var stream = await storage.OpenBlobReadAsync("nested/example.pdf", Revision, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(HttpMethod.Put, handler.Methods);
        Assert.Equal(handler.Bytes.Length, stream.Length);
        using var output = new MemoryStream();
        await stream.CopyToAsync(output, TestContext.Current.CancellationToken);
        Assert.Equal(handler.Bytes, output.ToArray());
        Assert.True(handler.Methods.Count(method => method == HttpMethod.Get) > 1);
        Assert.All(handler.Conditions, condition => Assert.Equal(Revision.ToString(), condition));
        Assert.Equal(HttpMethod.Head, handler.Methods[0]);
    }

    [Theory]
    [InlineData(404)]
    [InlineData(412)]
    [InlineData(503)]
    public async Task OpeningFailurePropagates(int status)
    {
        using var handler = new BlobHandler { FailureStatus = status };
        using var http = new HttpClient(handler);
        var exception = await Assert.ThrowsAsync<RequestFailedException>(() =>
            Create(http).OpenBlobReadAsync("example.pdf", Revision, TestContext.Current.CancellationToken));
        Assert.Equal(status, exception.Status);
    }

    [Fact]
    public async Task RevisionChangeDuringRead()
    {
        using var handler = new BlobHandler { FailSecondRange = true };
        using var http = new HttpClient(handler);
        await using var stream = await Create(http).OpenBlobReadAsync("example.pdf", Revision, TestContext.Current.CancellationToken);
        using var output = new MemoryStream();
        var exception = await Assert.ThrowsAsync<RequestFailedException>(() =>
            stream.CopyToAsync(output, TestContext.Current.CancellationToken));
        Assert.Equal(412, exception.Status);
        Assert.All(handler.Conditions, condition => Assert.Equal(Revision.ToString(), condition));
    }

    [Theory]
    [InlineData("")]
    [InlineData("*")]
    [InlineData("W/\"weak\"")]
    [InlineData("\"one\",\"two\"")]
    [InlineData("invalid\r\nheader")]
    public async Task RequiresExactRevision(string tag)
    {
        using var handler = new BlobHandler();
        using var http = new HttpClient(handler);
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            Create(http).OpenBlobReadAsync("example.pdf", new ETag(tag), TestContext.Current.CancellationToken));
        Assert.Empty(handler.Conditions);
    }

    [Fact]
    public async Task CancellationBeforeRequest()
    {
        using var handler = new BlobHandler();
        using var http = new HttpClient(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Create(http).OpenBlobReadAsync("example.pdf", Revision, cancellation.Token));
        Assert.Empty(handler.Conditions);
    }

    [Fact]
    public async Task CancellationOnRead()
    {
        using var handler = new BlobHandler();
        using var http = new HttpClient(handler);
        await using var stream = await Create(http).OpenBlobReadAsync("example.pdf", Revision, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            stream.ReadExactlyAsync(new byte[1], cancellation.Token).AsTask());
        Assert.DoesNotContain(HttpMethod.Get, handler.Methods);
    }

    private static TestStorage Create(HttpClient http) => new(new BlobContainerClient(
        new Uri("https://storage.example.com/documents"), new BlobClientOptions
        {
            Transport = new HttpClientTransport(http),
            Retry = { MaxRetries = 0 },
        }));

    private sealed class TestStorage(BlobContainerClient containerClient) : AzBlobStorageBase(containerClient);

    private sealed class BlobHandler : HttpMessageHandler
    {
        public byte[] Bytes { get; } = Enumerable.Range(0, 131_073).Select(index => (byte)(index % 251)).ToArray();
        public List<string?> Conditions { get; } = [];
        public List<HttpMethod> Methods { get; } = [];
        public int FailureStatus { get; init; }
        public bool FailSecondRange { get; init; }
        private int _rangeCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Conditions.Add(request.Headers.IfMatch.SingleOrDefault()?.ToString());
            Methods.Add(request.Method);
            if (FailureStatus != 0 || (request.Method == HttpMethod.Get && ++_rangeCount > 1 && FailSecondRange))
            {
                var status = FailureStatus == 0 ? 412 : FailureStatus;
                var failure = new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent("<Error><Code>ConditionNotMet</Code></Error>") };
                failure.Headers.Add("x-ms-error-code", "ConditionNotMet");
                return Task.FromResult(failure);
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.ETag = new EntityTagHeaderValue(Revision.ToString());
            response.Headers.Add("x-ms-blob-type", "BlockBlob");
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentLength = Bytes.Length;
            response.Content.Headers.LastModified = DateTimeOffset.UnixEpoch;
            if (request.Method == HttpMethod.Get)
            {
                var range = request.Headers.TryGetValues("x-ms-range", out var values)
                    ? values.Single() : request.Headers.Range!.ToString();
                var bounds = range["bytes=".Length..].Split('-');
                var start = int.Parse(bounds[0], CultureInfo.InvariantCulture);
                var end = Math.Min(int.Parse(bounds[1], CultureInfo.InvariantCulture), Bytes.Length - 1);
                response.StatusCode = HttpStatusCode.PartialContent;
                response.Content.Dispose();
                response.Content = new ByteArrayContent(Bytes[start..(end + 1)]);
                response.Content.Headers.ContentRange = new ContentRangeHeaderValue(start, end, Bytes.Length);
            }
            return Task.FromResult(response);
        }
    }
}
