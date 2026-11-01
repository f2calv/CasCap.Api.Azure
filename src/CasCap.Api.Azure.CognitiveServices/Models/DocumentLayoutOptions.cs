using System.ComponentModel.DataAnnotations;

namespace CasCap.Models;

/// <summary>Bounds a Document Intelligence layout request and its response.</summary>
public sealed record DocumentLayoutOptions
{
    /// <summary>Gets the maximum PDF payload size in bytes, up to 25 MiB.</summary>
    [Range(1, 25 * 1024 * 1024)]
    public int MaximumPdfBytes { get; init; } = 25 * 1024 * 1024;

    /// <summary>Gets the maximum result size in bytes, up to 16 MiB.</summary>
    [Range(1, 16 * 1024 * 1024)]
    public int MaximumResponseBytes { get; init; } = 16 * 1024 * 1024;

    /// <summary>Gets the maximum page number and selected-page count, up to 2000.</summary>
    [Range(1, 2000)]
    public int MaximumPages { get; init; } = 2000;

    /// <summary>Gets the clock used to interpret absolute Retry-After headers.</summary>
    [Required]
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;
}
