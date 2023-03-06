namespace CasCap.Models;

public class AppInsightsOptions
{
    public const string SectionKey = $"{nameof(CasCap)}:{nameof(AppInsightsOptions)}";
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    public string InstrumentationKey { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
}
