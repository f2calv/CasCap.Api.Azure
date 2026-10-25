namespace CasCap.Models;

/// <summary>A Blob Storage notification without SDK-specific payload types.</summary>
public sealed record BlobStorageEvent
{
    /// <summary>Gets the Event Grid event identity.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the Azure resource topic.</summary>
    public required string Topic { get; init; }

    /// <summary>Gets the storage event subject.</summary>
    public required string Subject { get; init; }

    /// <summary>Gets the source blob URI, not a caller-authorized download URL.</summary>
    public required Uri BlobUri { get; init; }

    /// <summary>Gets the per-blob hexadecimal ordering token.</summary>
    public required string Sequencer { get; init; }

    /// <summary>Gets the blob ETag when supplied by the storage event.</summary>
    public string? ETag { get; init; }

    /// <summary>Gets whether the event reports blob deletion rather than creation.</summary>
    public bool Deleted { get; init; }
}
