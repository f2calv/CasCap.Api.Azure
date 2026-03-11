using System.ComponentModel.DataAnnotations;

namespace CasCap.Models;

/// <summary>
/// Configuration options for Azure Cognitive Services.
/// Bound from the <c>CasCap:CognitiveServicesOptions</c> configuration section.
/// </summary>
public record CognitiveServicesOptions
{
    /// <summary>The configuration section path used to bind these options.</summary>
    public const string ConfigurationSectionName = $"{nameof(CasCap)}:{nameof(CognitiveServicesOptions)}";

    /// <summary>Gets the Cognitive Services subscription key used to authenticate API requests.</summary>
    [Required]
    public required string SubscriptionKey { get; init; }
}
