using System.ComponentModel.DataAnnotations;

namespace CasCap.Models;

/// <summary>Configuration options for Azure Application Insights.</summary>
public record AppInsightsOptions
{
    /// <summary>The configuration section path used to bind these options.</summary>
    public const string ConfigurationSectionName = $"{nameof(CasCap)}:{nameof(AppInsightsOptions)}";

    /// <summary>Gets the Application Insights instrumentation key.</summary>
    [Required]
    public required string InstrumentationKey { get; init; }
}
