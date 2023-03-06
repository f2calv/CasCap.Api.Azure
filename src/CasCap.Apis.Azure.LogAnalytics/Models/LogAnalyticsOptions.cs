namespace CasCap.Models;

public class LogAnalyticsOptions
{
    public const string SectionKey = $"{nameof(CasCap)}:{nameof(LogAnalyticsOptions)}";
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    public string WorkspaceId { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
}
