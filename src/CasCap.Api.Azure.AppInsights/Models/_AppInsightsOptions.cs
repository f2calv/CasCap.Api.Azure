namespace CasCap.Models;

public class AppInsightsOptions
{
    public const string SectionKey = $"{nameof(CasCap)}:{nameof(AppInsightsOptions)}";
    public string InstrumentationKey { get; set; } = default!;
}
