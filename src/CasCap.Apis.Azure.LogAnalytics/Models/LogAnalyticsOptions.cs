namespace CasCap.Models;

public class LogAnalyticsOptions
{
    public const string SectionKey = $"{nameof(CasCap)}:{nameof(LogAnalyticsOptions)}";
    public string WorkspaceId { get; set; } = string.Empty!;
}
