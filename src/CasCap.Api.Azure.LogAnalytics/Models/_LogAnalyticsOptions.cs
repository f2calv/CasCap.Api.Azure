using System.ComponentModel.DataAnnotations;

namespace CasCap.Models;

public record LogAnalyticsOptions
{
    public const string SectionKey = $"{nameof(CasCap)}:{nameof(LogAnalyticsOptions)}";

    [Required]
    public required string WorkspaceId { get; init; }
}
