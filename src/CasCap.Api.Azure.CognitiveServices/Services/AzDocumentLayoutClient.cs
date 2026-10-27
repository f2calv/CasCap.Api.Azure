using CasCap.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CasCap.Services;

/// <summary>Submits layout analysis once or resumes one recorded operation with a single GET.</summary>
/// <remarks>
/// Authentication and deadlines belong to the supplied client and caller. Its handlers must disable redirects,
/// retries, hedging and authentication replay for POST. This class neither logs nor owns the supplied client.
/// </remarks>
public sealed class AzDocumentLayoutClient
{
    /// <summary>The pinned Document Intelligence REST version.</summary>
    public const string ApiVersion = "2024-11-30";

    /// <summary>The layout model used for Markdown extraction.</summary>
    public const string ModelId = "prebuilt-layout";

    private const string ResultPath = "/documentintelligence/documentModels/" + ModelId + "/analyzeResults/";
    private readonly HttpClient _httpClient;
    private readonly DocumentLayoutOptions _options;
    private readonly Uri _endpoint;

    /// <summary>Creates a bounded transport over a caller-owned authenticated HTTP client.</summary>
    public AzDocumentLayoutClient(HttpClient httpClient, DocumentLayoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        if (options.MaximumPdfBytes is < 1 or > 25 * 1024 * 1024
            || options.MaximumResponseBytes is < 1 or > 16 * 1024 * 1024
            || options.MaximumPages is < 1 or > 2000 || options.TimeProvider is null)
            throw new ArgumentException("Layout limits must be positive and within the supported bounds.", nameof(options));
        _endpoint = ValidateEndpoint(httpClient.BaseAddress);
        _httpClient = httpClient;
        _options = options;
    }

    /// <summary>Submits exactly one POST and returns its validated operation receipt.</summary>
    /// <remarks>A failed submission may already have been accepted remotely; never automatically resubmit it.</remarks>
    public async Task<Uri> SubmitAsync(byte[] pdf, int[] pages, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(pdf);
        var selectedPages = ValidatePages(pages);
        if (pdf.Length < 5 || pdf.Length > _options.MaximumPdfBytes || !pdf.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
            throw new ArgumentException("Layout submission requires a bounded PDF payload.", nameof(pdf));
        var path = $"documentintelligence/documentModels/{ModelId}:analyze?api-version={ApiVersion}"
            + "&outputContentFormat=markdown&stringIndexType=utf16CodeUnit&pages="
            + string.Join(",", selectedPages.Select(page => page.ToString(CultureInfo.InvariantCulture)));
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_endpoint, path))
        {
            Content = JsonContent.Create(new { base64Source = Convert.ToBase64String(pdf) }),
        };
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Accepted)
            throw new HttpRequestException("Layout submission was not accepted; remote outcome may be unknown.",
                null, response.StatusCode);
        var locations = response.Headers.TryGetValues("Operation-Location", out var values) ? values.ToArray() : [];
        if (locations.Length != 1 || !Uri.TryCreate(locations[0], UriKind.Absolute, out var operation))
            throw ProtocolError();
        ValidateOperation(operation);
        return operation;
    }

    /// <summary>Gets one status observation for an existing operation, without polling or resubmitting.</summary>
    public async Task<DocumentLayoutPollResult> GetResultAsync(
        Uri operation,
        int[] expectedPages,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateOperation(operation);
        var selectedPages = ValidatePages(expectedPages);
        using var request = new HttpRequestMessage(HttpMethod.Get, operation);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException("Layout status request failed; resume the saved receipt, not submission.",
                null, response.StatusCode);
        try
        {
            var retryAfter = GetRetryAfter(response);
            using var json = await ReadResultAsync(response.Content, cancellationToken).ConfigureAwait(false);
            var root = json.RootElement;
            return root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.String
                ? throw ProtocolError()
                : status.GetString() switch
                {
                    "notStarted" or "running" => new() { Status = "Running", RetryAfter = retryAfter },
                    "failed" or "canceled" => new() { Status = "Failed", RetryAfter = retryAfter },
                    "succeeded" => ReadSucceeded(root, selectedPages, retryAfter),
                    _ => throw ProtocolError(),
                };
        }
        catch (JsonException)
        {
            throw ProtocolError();
        }
        catch (IOException)
        {
            throw ProtocolError();
        }
        catch (InvalidOperationException)
        {
            throw ProtocolError();
        }
        catch (FormatException)
        {
            throw ProtocolError();
        }
        catch (HttpRequestException)
        {
            throw ProtocolError();
        }
        catch (OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new HttpRequestException("Layout response timed out; resume the saved receipt.");
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            throw new HttpRequestException("Layout transport failed; remote outcome may be unknown.");
        }
        catch (IOException)
        {
            throw new HttpRequestException("Layout transport failed; remote outcome may be unknown.");
        }
        catch (OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new HttpRequestException("Layout transport timed out; remote outcome may be unknown.");
        }
    }

    private static Uri ValidateEndpoint(Uri? endpoint) =>
        endpoint is null || !endpoint.IsAbsoluteUri || endpoint.Scheme != Uri.UriSchemeHttps
            || endpoint.Port != 443 || endpoint.AbsolutePath != "/" || endpoint.HostNameType != UriHostNameType.Dns
            || endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0 || endpoint.UserInfo.Length != 0
            || endpoint.GetLeftPart(UriPartial.Authority).Contains('@')
            || endpoint.OriginalString != endpoint.AbsoluteUri
            ? throw new ArgumentException("Configure a canonical credential-free HTTPS root endpoint.")
            : endpoint;

    private void ValidateOperation(Uri operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (!operation.IsAbsoluteUri || operation.Scheme != Uri.UriSchemeHttps
            || operation.GetLeftPart(UriPartial.Authority) != _endpoint.GetLeftPart(UriPartial.Authority)
            || operation.UserInfo.Length != 0 || operation.Fragment.Length != 0
            || operation.OriginalString != operation.AbsoluteUri
            || !operation.AbsolutePath.StartsWith(ResultPath, StringComparison.Ordinal)
            || !Guid.TryParseExact(operation.AbsolutePath[ResultPath.Length..], "D", out _)
            || operation.Query != $"?api-version={ApiVersion}")
            throw ProtocolError();
    }

    private int[] ValidatePages(int[] pages)
    {
        ArgumentNullException.ThrowIfNull(pages);
        var snapshot = (int[])pages.Clone();
        return snapshot.Length < 1 || snapshot.Length > _options.MaximumPages
            || snapshot.Any(page => page < 1 || page > _options.MaximumPages)
            || snapshot.Distinct().Count() != snapshot.Length
            ? throw new ArgumentException("Layout pages must be distinct positive numbers within the configured limit.",
                nameof(pages))
            : snapshot;
    }

    private TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Retry-After", out var values))
            return null;
        if (values.Count() != 1 || response.Headers.RetryAfter is not { } retry)
            throw ProtocolError();
        var delay = retry.Delta ?? (retry.Date - _options.TimeProvider.GetUtcNow());
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }

    private async Task<JsonDocument> ReadResultAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > _options.MaximumResponseBytes
            || content.Headers.ContentType?.MediaType is not "application/json")
            throw ProtocolError();
        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0,
            (int)Math.Min(buffer.Length, _options.MaximumResponseBytes - output.Length + 1)), cancellationToken)
            .ConfigureAwait(false)) > 0)
        {
            if (output.Length + read > _options.MaximumResponseBytes)
                throw ProtocolError();
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
        cancellationToken.ThrowIfCancellationRequested();
        var json = JsonDocument.Parse(output.GetBuffer().AsMemory(0, (int)output.Length));
        try
        {
            ValidateUniqueProperties(json.RootElement);
            return json;
        }
        catch
        {
            json.Dispose();
            throw;
        }
    }

    private static void ValidateUniqueProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw ProtocolError();
                ValidateUniqueProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray())
                ValidateUniqueProperties(item);
    }

    private static DocumentLayoutPollResult ReadSucceeded(JsonElement root, int[] expectedPages, TimeSpan? retryAfter)
    {
        if (!root.TryGetProperty("analyzeResult", out var result) || result.ValueKind != JsonValueKind.Object
            || !HasString(result, "apiVersion", ApiVersion) || !HasString(result, "modelId", ModelId)
            || !HasString(result, "contentFormat", "markdown")
            || !result.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.String
            || !result.TryGetProperty("pages", out var pages) || pages.ValueKind != JsonValueKind.Array
            || pages.GetArrayLength() != expectedPages.Length)
            throw ProtocolError();
        var markdown = content.GetString()!;
        var projected = new Dictionary<int, string>();
        var occupied = new List<(int Start, int Length)>();
        var uniqueSpans = new HashSet<(int Start, int Length)>();
        foreach (var page in pages.EnumerateArray())
        {
            if (page.ValueKind != JsonValueKind.Object
                || !page.TryGetProperty("pageNumber", out var number) || number.ValueKind != JsonValueKind.Number
                || !number.TryGetInt32(out var pageNumber)
                || !expectedPages.Contains(pageNumber) || projected.ContainsKey(pageNumber)
                || !page.TryGetProperty("spans", out var spans) || spans.ValueKind != JsonValueKind.Array)
                throw ProtocolError();
            var fragments = new List<(int Start, int Length)>();
            foreach (var span in spans.EnumerateArray())
            {
                if (span.ValueKind != JsonValueKind.Object
                    || !span.TryGetProperty("offset", out var offset) || offset.ValueKind != JsonValueKind.Number
                    || !offset.TryGetInt32(out var start)
                    || !span.TryGetProperty("length", out var length) || length.ValueKind != JsonValueKind.Number
                    || !length.TryGetInt32(out var count)
                    || start < 0 || count < 0 || start > markdown.Length - count
                    || SplitsSurrogate(markdown, start) || SplitsSurrogate(markdown, start + count)
                    || !uniqueSpans.Add((start, count)))
                    throw ProtocolError();
                fragments.Add((start, count));
                if (count > 0)
                    occupied.Add((start, count));
            }
            fragments.Sort((left, right) => left.Start.CompareTo(right.Start));
            projected.Add(pageNumber, string.Concat(fragments.Select(span => markdown.Substring(span.Start, span.Length))));
        }
        occupied.Sort((left, right) => left.Start.CompareTo(right.Start));
        for (var index = 1; index < occupied.Count; index++)
            if (occupied[index].Start < occupied[index - 1].Start + occupied[index - 1].Length)
                throw ProtocolError();
        return new()
        {
            Status = "Succeeded",
            Markdown = markdown,
            Pages = new ReadOnlyDictionary<int, string>(projected),
            RetryAfter = retryAfter,
        };
    }

    private static bool SplitsSurrogate(string text, int boundary) =>
        boundary > 0 && boundary < text.Length && char.IsHighSurrogate(text[boundary - 1])
        && char.IsLowSurrogate(text[boundary]);

    private static bool HasString(JsonElement element, string property, string expected) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
        && value.GetString() == expected;

    private static HttpRequestException ProtocolError() =>
        new("Layout response violates the bounded operation contract; service details withheld.");
}
