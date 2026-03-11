namespace CasCap.Abstractions;

/// <summary>Abstraction for querying Azure Monitor / Application Insights via Log Analytics.</summary>
public interface ILogAnalyticsQueryService
{
    /// <summary>Returns up to <paramref name="limit"/> recent exception records from the workspace.</summary>
    Task<List<AppInsightsObject>> GetExceptions(int limit = 50);
}
