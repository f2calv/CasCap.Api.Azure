using System.ComponentModel.DataAnnotations;

namespace CasCap.Models;

public record CognitiveServicesOptions
{
    public const string SectionKey = $"{nameof(CasCap)}:{nameof(CognitiveServicesOptions)}";

    [Required]
    public required string SubscriptionKey { get; init; }
}
