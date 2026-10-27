namespace CasCap.Models;

/// <summary>Represents one layout status observation, without waiting or resubmitting.</summary>
public sealed record DocumentLayoutPollResult
{
    /// <summary>Gets the normalized status: Running, Succeeded or Failed.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the complete Markdown on success; otherwise null.</summary>
    public string? Markdown { get; init; }

    /// <summary>Gets exact UTF-16 page projections on success; otherwise an empty dictionary.</summary>
    public IReadOnlyDictionary<int, string> Pages { get; init; } =
        new System.Collections.ObjectModel.ReadOnlyDictionary<int, string>(new Dictionary<int, string>());

    /// <summary>Gets the server's nonnegative retry interval, or null when absent.</summary>
    public TimeSpan? RetryAfter { get; init; }
}
