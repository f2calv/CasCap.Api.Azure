using CasCap.Models;
using CasCap.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CasCap.Tests;

/// <summary>Exercises the layout wire contract using synthetic in-memory HTTP responses only.</summary>
[Trait("Category", "DocumentLayout")]
public sealed class AzDocumentLayoutClientTests
{
    private const string Endpoint = "https://example.com/";
    private const string Receipt = Endpoint
        + "documentintelligence/documentModels/prebuilt-layout/analyzeResults/00000000-0000-0000-0000-000000000001"
        + "?api-version=2024-11-30";
    private static readonly byte[] Pdf = "%PDF-synthetic"u8.ToArray();

    [Fact]
    public async Task SubmitAsync()
    {
        using var handler = new ScriptedHandler(async (request, token) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(Endpoint + "documentintelligence/documentModels/prebuilt-layout:analyze"
                + "?api-version=2024-11-30&outputContentFormat=markdown&stringIndexType=utf16CodeUnit&pages=3,1",
                request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("application/json", request.Content!.Headers.ContentType?.MediaType);
            using var json = JsonDocument.Parse(await request.Content.ReadAsStringAsync(token));
            Assert.Single(json.RootElement.EnumerateObject());
            Assert.Equal(Convert.ToBase64String(Pdf), json.RootElement.GetProperty("base64Source").GetString());
            return Accepted();
        });
        using var http = CreateHttp(handler);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-secret");
        var client = new AzDocumentLayoutClient(http, new());
        Assert.Equal(new Uri(Receipt), await client.SubmitAsync(Pdf, [3, 1], CancellationToken.None));
        Assert.Equal(1, handler.Attempts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://example.com/")]
    [InlineData("https://example.com:444/")]
    [InlineData("https://example.com/path/")]
    [InlineData("https://example.com/?key=synthetic-secret")]
    [InlineData("https://example.com/#fragment")]
    [InlineData("https://user:synthetic-secret@example.com/")]
    [InlineData("https://example.com")]
    [InlineData("https://example.com/a/../")]
    [InlineData("https://127.0.0.1/")]
    [InlineData("https://@example.com/")]
    public void EndpointValidation(string? endpoint)
    {
        using var handler = Handler(Accepted);
        using var http = CreateHttp(handler);
        http.BaseAddress = endpoint is null ? null : new Uri(endpoint);
        var error = Assert.Throws<ArgumentException>(() => new AzDocumentLayoutClient(http, new()));
        Assert.DoesNotContain("synthetic-secret", error.ToString());
        Assert.Equal(0, handler.Attempts);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(-1, 1, 1)]
    [InlineData(26214401, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 16777217, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, 2001)]
    public void OptionsValidation(int pdf, int response, int pages)
    {
        using var handler = Handler(Accepted);
        using var http = CreateHttp(handler);
        Assert.Throws<ArgumentException>(() => new AzDocumentLayoutClient(http, new()
        {
            MaximumPdfBytes = pdf,
            MaximumResponseBytes = response,
            MaximumPages = pages,
        }));
        Assert.Equal(0, handler.Attempts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1,1")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("2001")]
    public async Task PageValidation(string selection)
    {
        using var handler = Handler(Accepted);
        using var http = CreateHttp(handler);
        var client = new AzDocumentLayoutClient(http, new());
        var pages = selection.Length == 0 ? [] : selection.Split(',').Select(int.Parse).ToArray();
        await Assert.ThrowsAsync<ArgumentException>(() => client.SubmitAsync(Pdf, pages, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetResultAsync(new(Receipt), pages, CancellationToken.None));
        Assert.Equal(0, handler.Attempts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("%PDF")]
    [InlineData("not-a-pdf")]
    public async Task PdfValidation(string payload)
    {
        using var handler = Handler(Accepted);
        using var http = CreateHttp(handler);
        var client = new AzDocumentLayoutClient(http, new());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.SubmitAsync(Encoding.UTF8.GetBytes(payload), [1], CancellationToken.None));
        Assert.Equal(0, handler.Attempts);
    }

    [Fact]
    public async Task CallerLimits()
    {
        using var handler = Handler(Accepted);
        using var http = CreateHttp(handler);
        var client = new AzDocumentLayoutClient(http, new());
        var pdf = new byte[25 * 1024 * 1024];
        Pdf.CopyTo(pdf, 0);
        Assert.Equal(new Uri(Receipt), await client.SubmitAsync(pdf, [.. Enumerable.Range(1, 2000)],
            CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => client.SubmitAsync(new byte[pdf.Length + 1], [1],
            CancellationToken.None));
        var restricted = new AzDocumentLayoutClient(http, new() { MaximumPdfBytes = 5, MaximumPages = 1 });
        await Assert.ThrowsAsync<ArgumentException>(() => restricted.SubmitAsync(Pdf, [1], CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => restricted.SubmitAsync("%PDF-"u8.ToArray(), [2],
            CancellationToken.None));
        Assert.Equal(1, handler.Attempts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("/relative")]
    [InlineData("https://other.example.com/documentintelligence/documentModels/prebuilt-layout/analyzeResults/00000000-0000-0000-0000-000000000001?api-version=2024-11-30")]
    [InlineData("https://example.com/documentintelligence/documentModels/prebuilt-layout/analyzeResults/not-a-guid?api-version=2024-11-30")]
    [InlineData("https://example.com/documentintelligence/documentModels/prebuilt-layout/analyzeResults/{00000000-0000-0000-0000-000000000001}?api-version=2024-11-30")]
    [InlineData("https://example.com/documentintelligence/documentModels/other/analyzeResults/00000000-0000-0000-0000-000000000001?api-version=2024-11-30")]
    [InlineData("https://example.com/documentintelligence/documentModels/prebuilt-layout/analyzeResults/00000000-0000-0000-0000-000000000001?api-version=other")]
    [InlineData("https://example.com/documentintelligence/documentModels/prebuilt-layout/analyzeResults/00000000-0000-0000-0000-000000000001?api-version=2024-11-30&key=synthetic-secret")]
    public async Task ReceiptValidation(string? receipt)
    {
        using var handler = Handler(() => Accepted(receipt));
        using var http = CreateHttp(handler);
        var client = new AzDocumentLayoutClient(http, new());
        var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.SubmitAsync(Pdf, [1], CancellationToken.None));
        Assert.DoesNotContain("synthetic-secret", error.ToString());
        Assert.Equal(1, handler.Attempts);
    }

    [Theory]
    [InlineData("http")]
    [InlineData("credentials")]
    [InlineData("fragment")]
    [InlineData("extra-path")]
    [InlineData("duplicate-query")]
    [InlineData("escaped")]
    [InlineData("dot-segment")]
    [InlineData("duplicate-header")]
    public async Task ReceiptCanonicalization(string mutation)
    {
        var receipt = mutation switch
        {
            "http" => Receipt.Replace("https:", "http:"),
            "credentials" => Receipt.Replace("https://", "https://user:synthetic-secret@"),
            "fragment" => Receipt + "#fragment",
            "extra-path" => Receipt.Replace("?api", "/extra?api"),
            "duplicate-query" => Receipt + "&api-version=2024-11-30",
            "escaped" => Receipt.Replace("analyzeResults", "%61nalyzeResults"),
            "dot-segment" => Receipt.Replace("/analyzeResults/", "/other/../analyzeResults/"),
            _ => Receipt,
        };
        using var handler = Handler(() =>
        {
            var response = Accepted(receipt);
            if (mutation == "duplicate-header")
                response.Headers.TryAddWithoutValidation("Operation-Location", Receipt);
            return response;
        });
        using var http = CreateHttp(handler);
        var client = new AzDocumentLayoutClient(http, new());
        await Assert.ThrowsAsync<HttpRequestException>(() => client.SubmitAsync(Pdf, [1], CancellationToken.None));
        if (mutation != "duplicate-header")
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                client.GetResultAsync(new(receipt), [1], CancellationToken.None));
        Assert.Equal(1, handler.Attempts);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(301)]
    [InlineData(401)]
    [InlineData(429)]
    [InlineData(503)]
    public async Task SubmissionSingleAttempt(int statusCode)
    {
        using var handler = Handler(() => new((HttpStatusCode)statusCode)
        {
            Content = new StringContent("synthetic-private-content"),
        });
        using var http = CreateHttp(handler);
        var client = new AzDocumentLayoutClient(http, new());
        var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.SubmitAsync(Pdf, [1], CancellationToken.None));
        Assert.Equal((HttpStatusCode)statusCode, error.StatusCode);
        Assert.DoesNotContain("synthetic-private-content", error.ToString());
        Assert.Equal(1, handler.Attempts);
    }

    [Theory]
    [InlineData("transport")]
    [InlineData("io")]
    [InlineData("timeout")]
    public async Task TransportSingleAttempt(string failure)
    {
        using var handler = new ScriptedHandler((_, _) => throw failure switch
        {
            "transport" => new HttpRequestException("synthetic-secret"),
            "io" => new IOException("synthetic-secret"),
            _ => new TaskCanceledException("synthetic-secret"),
        });
        using var http = CreateHttp(handler);
        var client = new AzDocumentLayoutClient(http, new());
        var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.SubmitAsync(Pdf, [1], CancellationToken.None));
        Assert.DoesNotContain("synthetic-secret", error.ToString());
        Assert.Null(error.InnerException);
        Assert.Equal(1, handler.Attempts);
    }

    [Theory]
    [InlineData("notStarted", "Running")]
    [InlineData("running", "Running")]
    [InlineData("failed", "Failed")]
    [InlineData("canceled", "Failed")]
    public async Task ResumedGet(string wireStatus, string status)
    {
        using var handler = new ScriptedHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(new Uri(Receipt), request.RequestUri);
            Assert.Null(request.Content);
            return Task.FromResult(JsonResponse(JsonSerializer.Serialize(new
            {
                status = wireStatus,
                error = new { message = "synthetic-secret" },
            })));
        });
        using var http = CreateHttp(handler);
        var result = await new AzDocumentLayoutClient(http, new()).GetResultAsync(new(Receipt), [1], CancellationToken.None);
        Assert.Equal(status, result.Status);
        Assert.Null(result.Markdown);
        Assert.Empty(result.Pages);
        Assert.Null(result.RetryAfter);
        Assert.DoesNotContain("synthetic-secret", result.ToString());
        Assert.Equal(1, handler.Attempts);
    }

    [Theory]
    [InlineData(429)]
    [InlineData(503)]
    [InlineData(202)]
    public async Task GetFailure(int status)
    {
        using var handler = Handler(() => new((HttpStatusCode)status)
        {
            Content = new StringContent("synthetic-secret"),
        });
        using var http = CreateHttp(handler);
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => new AzDocumentLayoutClient(http, new())
            .GetResultAsync(new(Receipt), [1], CancellationToken.None));
        Assert.DoesNotContain("synthetic-secret", error.ToString());
        Assert.Equal((HttpStatusCode)status, error.StatusCode);
        Assert.Equal(1, handler.Attempts);
    }

    [Theory]
    [InlineData("12", 12)]
    [InlineData("0", 0)]
    [InlineData("Thu, 01 Jan 2026 00:00:20 GMT", 20)]
    [InlineData("Wed, 31 Dec 2025 23:59:59 GMT", 0)]
    public async Task RetryAfter(string header, int seconds)
    {
        using var handler = Handler(() =>
        {
            var response = JsonResponse("""{"status":"running"}""");
            response.Headers.TryAddWithoutValidation("Retry-After", header);
            return response;
        });
        using var http = CreateHttp(handler);
        var result = await new AzDocumentLayoutClient(http, new() { TimeProvider = new FixedClock() })
            .GetResultAsync(new(Receipt), [1], CancellationToken.None);
        Assert.Equal(TimeSpan.FromSeconds(seconds), result.RetryAfter);
        Assert.Equal(1, handler.Attempts);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{")]
    [InlineData("""{"status":42}""")]
    [InlineData("""{"status":null}""")]
    [InlineData("""{"status":"unknown"}""")]
    [InlineData("""{"status":"Succeeded"}""")]
    [InlineData("""{"status":"succeeded"}""")]
    [InlineData("""{"status":"running","status":"succeeded"}""")]
    [InlineData("""{"status":"running","error":{"code":"a","code":"b"}}""")]
    public async Task MalformedStatus(string payload)
    {
        using var handler = Handler(() => JsonResponse(payload));
        using var http = CreateHttp(handler);
        await Assert.ThrowsAsync<HttpRequestException>(() => new AzDocumentLayoutClient(http, new())
            .GetResultAsync(new(Receipt), [1], CancellationToken.None));
        Assert.Equal(1, handler.Attempts);
    }

    [Fact]
    public async Task SuccessfulProjections()
    {
        var result = Success();
        result["analyzeResult"]!["content"] = "A😀B\nC";
        result["analyzeResult"]!["pages"] = JsonNode.Parse(
            """[{"pageNumber":1,"spans":[{"offset":4,"length":2},{"offset":0,"length":4}]},{"pageNumber":2,"spans":[]}]""");
        using var handler = Handler(() => JsonResponse(result.ToJsonString()));
        using var http = CreateHttp(handler);
        var actual = await new AzDocumentLayoutClient(http, new()).GetResultAsync(new(Receipt), [2, 1], CancellationToken.None);
        Assert.Equal("Succeeded", actual.Status);
        Assert.Equal("A😀B\nC", actual.Markdown);
        Assert.Equal("A😀B\nC", actual.Pages[1]);
        Assert.Equal("", actual.Pages[2]);
        Assert.Equal(2, actual.Pages.Count);
        Assert.Equal(1, handler.Attempts);
    }

    [Fact]
    public async Task BlankDocument()
    {
        var result = Success();
        result["analyzeResult"]!["content"] = "";
        result["analyzeResult"]!["pages"]![0]!["spans"] = new JsonArray();
        using var handler = Handler(() => JsonResponse(result.ToJsonString()));
        using var http = CreateHttp(handler);
        var actual = await new AzDocumentLayoutClient(http, new()).GetResultAsync(new(Receipt), [1], CancellationToken.None);
        Assert.Equal("", actual.Markdown);
        Assert.Equal("", actual.Pages[1]);
        Assert.Equal("Succeeded", actual.Status);
    }

    [Theory]
    [InlineData("apiVersion", "other")]
    [InlineData("modelId", "other")]
    [InlineData("contentFormat", "text")]
    [InlineData("content", null)]
    [InlineData("pages", null)]
    public async Task ResultContract(string property, string? value)
    {
        var result = Success();
        result["analyzeResult"]![property] = value;
        using var handler = Handler(() => JsonResponse(result.ToJsonString()));
        using var http = CreateHttp(handler);
        await Assert.ThrowsAsync<HttpRequestException>(() => new AzDocumentLayoutClient(http, new())
            .GetResultAsync(new(Receipt), [1], CancellationToken.None));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("""[{"pageNumber":2,"spans":[]}]""")]
    [InlineData("""[{"pageNumber":"1","spans":[]}]""")]
    [InlineData("""[{"pageNumber":1}]""")]
    [InlineData("""[{"pageNumber":1,"spans":null}]""")]
    [InlineData("""[{"pageNumber":1,"spans":[]},{"pageNumber":1,"spans":[]}]""")]
    public async Task ResultPages(string pages)
    {
        var result = Success();
        result["analyzeResult"]!["pages"] = JsonNode.Parse(pages);
        using var handler = Handler(() => JsonResponse(result.ToJsonString()));
        using var http = CreateHttp(handler);
        await Assert.ThrowsAsync<HttpRequestException>(() => new AzDocumentLayoutClient(http, new())
            .GetResultAsync(new(Receipt), [1], CancellationToken.None));
    }

    [Theory]
    [InlineData("""[{"offset":-1,"length":1}]""")]
    [InlineData("""[{"offset":0,"length":-1}]""")]
    [InlineData("""[{"offset":0,"length":2147483647}]""")]
    [InlineData("""[{"offset":2147483647,"length":1}]""")]
    [InlineData("""[{"offset":"0","length":1}]""")]
    [InlineData("""[{"offset":0}]""")]
    [InlineData("""[{"offset":2,"length":1}]""")]
    [InlineData("""[{"offset":1,"length":1}]""")]
    [InlineData("""[{"offset":0,"length":3},{"offset":2,"length":2}]""")]
    [InlineData("""[{"offset":0,"length":1},{"offset":0,"length":1}]""")]
    [InlineData("""[{"offset":0,"length":0},{"offset":0,"length":0}]""")]
    public async Task ResultSpans(string spans)
    {
        var result = Success();
        result["analyzeResult"]!["content"] = "A😀B";
        result["analyzeResult"]!["pages"]![0]!["spans"] = JsonNode.Parse(spans);
        using var handler = Handler(() => JsonResponse(result.ToJsonString()));
        using var http = CreateHttp(handler);
        await Assert.ThrowsAsync<HttpRequestException>(() => new AzDocumentLayoutClient(http, new())
            .GetResultAsync(new(Receipt), [1], CancellationToken.None));
    }

    [Fact]
    public async Task CrossPageOverlap()
    {
        var result = Success();
        result["analyzeResult"]!["pages"] = JsonNode.Parse(
            """[{"pageNumber":1,"spans":[{"offset":0,"length":2}]},{"pageNumber":2,"spans":[{"offset":1,"length":2}]}]""");
        using var handler = Handler(() => JsonResponse(result.ToJsonString()));
        using var http = CreateHttp(handler);
        await Assert.ThrowsAsync<HttpRequestException>(() => new AzDocumentLayoutClient(http, new())
            .GetResultAsync(new(Receipt), [1, 2], CancellationToken.None));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task ResponseBounds(bool contentLength, bool exact)
    {
        var payload = Encoding.UTF8.GetBytes("""{"status":"running"}""");
        using var handler = Handler(() =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = contentLength ? new ByteArrayContent(payload)
                    : new StreamContent(new NonSeekableStream(payload)),
            };
            response.Content.Headers.ContentType = new("application/json");
            return response;
        });
        using var http = CreateHttp(handler);
        var client = new AzDocumentLayoutClient(http, new() { MaximumResponseBytes = payload.Length - (exact ? 0 : 1) });
        if (exact)
            Assert.Equal("Running", (await client.GetResultAsync(new(Receipt), [1], CancellationToken.None)).Status);
        else
            await Assert.ThrowsAsync<HttpRequestException>(() => client.GetResultAsync(new(Receipt), [1], CancellationToken.None));
        Assert.Equal(1, handler.Attempts);
    }

    [Fact]
    public async Task MaximumResultSize()
    {
        using var handler = Handler(() => JsonResponse("""{"status":"running"}""".PadRight(16 * 1024 * 1024)));
        using var http = CreateHttp(handler);
        var result = await new AzDocumentLayoutClient(http, new())
            .GetResultAsync(new(Receipt), [1], CancellationToken.None);
        Assert.Equal("Running", result.Status);
        Assert.Equal(1, handler.Attempts);
    }

    [Fact]
    public async Task BodyCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        using var stream = new CancelingStream(cancellation);
        using var handler = Handler(() =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) };
            response.Content.Headers.ContentType = new("application/json");
            return response;
        });
        using var http = CreateHttp(handler);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new AzDocumentLayoutClient(http, new())
            .GetResultAsync(new(Receipt), [1], cancellation.Token));
        Assert.Equal(1, handler.Attempts);
        Assert.False(stream.CanRead);
    }

    [Fact]
    public async Task BodyFailureRedaction()
    {
        using var stream = new FailingStream();
        using var handler = Handler(() =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) };
            response.Content.Headers.ContentType = new("application/json");
            return response;
        });
        using var http = CreateHttp(handler);
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => new AzDocumentLayoutClient(http, new())
            .GetResultAsync(new(Receipt), [1], CancellationToken.None));
        Assert.DoesNotContain("synthetic-secret", error.ToString());
        Assert.Null(error.InnerException);
        Assert.False(stream.CanRead);
    }

    [Theory]
    [InlineData("content-type")]
    [InlineData("retry-after")]
    [InlineData("duplicate-retry")]
    public async Task ResponseHeaders(string failure)
    {
        using var handler = Handler(() =>
        {
            var response = JsonResponse("""{"status":"running"}""");
            if (failure == "content-type")
                response.Content.Headers.ContentType = new("text/html");
            else
                response.Headers.TryAddWithoutValidation("Retry-After",
                    failure == "retry-after" ? ["synthetic-secret"] : ["1", "2"]);
            return response;
        });
        using var http = CreateHttp(handler);
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => new AzDocumentLayoutClient(http, new())
            .GetResultAsync(new(Receipt), [1], CancellationToken.None));
        Assert.DoesNotContain("synthetic-secret", error.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation(bool duringSend)
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new ScriptedHandler(async (_, token) =>
        {
            cancellation.Cancel();
            await Task.Delay(Timeout.Infinite, token);
            return Accepted();
        });
        using var http = CreateHttp(handler);
        var client = new AzDocumentLayoutClient(http, new());
        if (!duringSend)
            cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.SubmitAsync(Pdf, [1], cancellation.Token));
        Assert.Equal(duringSend ? 1 : 0, handler.Attempts);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetResultAsync(new(Receipt), [1], cancellation.Token));
        Assert.Equal(duringSend ? 1 : 0, handler.Attempts);
    }

    private static HttpClient CreateHttp(HttpMessageHandler handler) => new(handler) { BaseAddress = new(Endpoint) };

    private static ScriptedHandler Handler(Func<HttpResponseMessage> response) =>
        new((_, _) => Task.FromResult(response()));

    private static HttpResponseMessage Accepted() => Accepted(Receipt);

    private static HttpResponseMessage Accepted(string? receipt)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Accepted);
        if (receipt is not null)
            response.Headers.TryAddWithoutValidation("Operation-Location", receipt);
        return response;
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static JsonObject Success() => JsonNode.Parse("""
        {"status":"succeeded","analyzeResult":{"apiVersion":"2024-11-30","modelId":"prebuilt-layout",
        "contentFormat":"markdown","content":"hello","pages":[{"pageNumber":1,"spans":[{"offset":0,"length":5}]}]}}
        """)!.AsObject();

    private sealed class ScriptedHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send
    ) : HttpMessageHandler
    {
        public int Attempts { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Attempts++;
            return send(request, cancellationToken);
        }
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
    }

    private sealed class CancelingStream(CancellationTokenSource cancellation) : MemoryStream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellation.Cancel();
            return ValueTask.FromCanceled<int>(cancellationToken);
        }
    }

    private sealed class FailingStream : MemoryStream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromException<int>(new IOException("synthetic-secret"));
    }
}
