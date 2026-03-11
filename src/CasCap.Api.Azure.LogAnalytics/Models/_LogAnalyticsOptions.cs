using System.ComponentModel.DataAnnotations;

namespace CasCap.Models;

/// <summary>Configuration options for Azure Monitor / Log Analytics.</summary>
public record LogAnalyticsOptions
{
    /// <summary>The configuration section path used to bind these options.</summary>
    public const string ConfigurationSectionName = $"{nameof(CasCap)}:{nameof(LogAnalyticsOptions)}";

    /// <summary>Gets the Log Analytics workspace ID.</summary>
    [Required]
    public required string WorkspaceId { get; init; }
}
