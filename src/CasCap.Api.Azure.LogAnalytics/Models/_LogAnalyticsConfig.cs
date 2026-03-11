using System.ComponentModel.DataAnnotations;

namespace CasCap.Models;

/// <summary>
/// Configuration options for Azure Monitor Log Analytics.
/// Bound from the <c>CasCap:LogAnalyticsConfig</c> configuration section.
/// </summary>
public record LogAnalyticsConfig
{
    /// <summary>The configuration section path used to bind these options.</summary>
    public const string ConfigurationSectionName = $"{nameof(CasCap)}:{nameof(LogAnalyticsConfig)}";

    /// <summary>Gets the Log Analytics workspace ID used to scope queries.</summary>
    [Required]
    public required string WorkspaceId { get; init; }
}
