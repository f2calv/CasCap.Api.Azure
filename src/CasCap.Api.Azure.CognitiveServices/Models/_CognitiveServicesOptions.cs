using System.ComponentModel.DataAnnotations;

namespace CasCap.Models;

public record CognitiveServicesOptions
{
    public const string ConfigurationSectionName = $"{nameof(CasCap)}:{nameof(CognitiveServicesOptions)}";

    [Required]
    public required string SubscriptionKey { get; init; }
}
