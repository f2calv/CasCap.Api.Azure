namespace CasCap.Abstractions;

/// <summary>Abstraction for querying Azure Monitor / Application Insights via Log Analytics.</summary>
public interface IQueryService
{
    /// <summary>Queries the workspace for up to 50 recent records across all tables and logs them.</summary>
    /// <param name="timeRange">The <see cref="Azure.Monitor.Query.QueryTimeRange"/> to constrain the query.</param>
    Task Query(QueryTimeRange timeRange);

    /// <summary>Returns up to <paramref name="limit"/> recent exception records from the workspace.</summary>
    Task<List<AppInsightsObject>> GetExceptions(int limit = 50);
}
