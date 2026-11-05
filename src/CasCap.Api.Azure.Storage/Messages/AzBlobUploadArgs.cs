namespace CasCap.Messages;

/// <summary>Optional properties written with a blob by <see cref="CasCap.Abstractions.IAzBlobStorageBase.UploadBlobIfNotExistsAsync"/>.</summary>
public sealed record AzBlobUploadArgs
{
    /// <summary>Gets the MIME type stored as the blob's content type, or <see langword="null"/> for the service default.</summary>
    public string? ContentType { get; init; }

    /// <summary>Gets the user metadata stored with the blob, or <see langword="null"/> for none.</summary>
    /// <remarks>Keys must be valid C# identifiers. Values are sent as HTTP headers, so keep them ASCII.</remarks>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>Gets the access tier for the new blob, or <see langword="null"/> for the account default.</summary>
    public AccessTier? AccessTier { get; init; }
}
