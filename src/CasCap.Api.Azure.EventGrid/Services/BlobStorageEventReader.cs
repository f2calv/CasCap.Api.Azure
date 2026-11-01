using Azure.Messaging.EventGrid;
using CasCap.Models;
using System.Text.Json;

namespace CasCap.Services;

/// <summary>Reads a single created/deleted Blob Storage event using the Event Grid delivery schema.</summary>
public static class BlobStorageEventReader
{
    /// <summary>Parses one notification; rejects unsupported schemas, event types and malformed ordering data.</summary>
    /// <remarks>The caller must authorize the topic, subject and source container before acting on the event.</remarks>
    public static BlobStorageEvent Read(BinaryData body)
    {
        ArgumentNullException.ThrowIfNull(body);
        var envelope = EventGridEvent.Parse(body)
            ?? throw new JsonException("A single Event Grid notification is required.");
        if (envelope.EventType is not ("Microsoft.Storage.BlobCreated" or "Microsoft.Storage.BlobDeleted"))
            throw new JsonException("Only BlobCreated and BlobDeleted notifications are supported.");
        using var data = JsonDocument.Parse(envelope.Data);
        if (data.RootElement.ValueKind != JsonValueKind.Object)
            throw new JsonException("Storage event data must be an object.");
        var url = ReadRequired(data.RootElement, "url");
        var sequencer = ReadRequired(data.RootElement, "sequencer");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
            throw new JsonException("A credential-free HTTPS blob URI is required.");
        if (sequencer.Length > 128 || !sequencer.All(Uri.IsHexDigit))
            throw new JsonException("A hexadecimal storage sequencer of at most 128 characters is required.");
        if (string.IsNullOrWhiteSpace(envelope.Id) || envelope.Id.Length > 200
            || string.IsNullOrWhiteSpace(envelope.Topic) || envelope.Topic.Length > 2_000
            || string.IsNullOrWhiteSpace(envelope.Subject) || envelope.Subject.Length > 4_000
            || uri.AbsoluteUri.Length > 4_000)
            throw new JsonException("The event identity or source exceeds supported bounds.");
        var etag = data.RootElement.TryGetProperty("eTag", out var property)
            && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
        return etag?.Length > 200
            ? throw new JsonException("The source ETag exceeds supported bounds.")
            : new BlobStorageEvent
            {
                Id = envelope.Id,
                Topic = envelope.Topic,
                Subject = envelope.Subject,
                BlobUri = uri,
                Sequencer = sequencer.ToUpperInvariant().PadLeft(128, '0'),
                ETag = etag,
                Deleted = envelope.EventType == "Microsoft.Storage.BlobDeleted",
            };
    }

    private static string ReadRequired(JsonElement data, string name) =>
        data.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(property.GetString())
            ? property.GetString()!
            : throw new JsonException("A required storage event field is missing.");
}
