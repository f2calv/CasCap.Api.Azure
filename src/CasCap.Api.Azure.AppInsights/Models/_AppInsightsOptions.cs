using System.ComponentModel.DataAnnotations;

namespace CasCap.Models;

public record AppInsightsOptions
{
    public const string SectionKey = $"{nameof(CasCap)}:{nameof(AppInsightsOptions)}";

    [Required]
    public required string InstrumentationKey { get; init; }
}
