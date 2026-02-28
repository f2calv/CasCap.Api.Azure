namespace CasCap.Abstractions;

/// <summary>
/// Defines the contract for querying Azure Monitor Log Analytics workspaces.
/// </summary>
public interface ILogAnalyticsQueryService
{
    //Task Query(string timespan = null);
    //Task GetCustomEvents(string timespan = null);
    /// <summary>
    /// Retrieves the most recent application exceptions from the Log Analytics workspace.
    /// </summary>
    /// <param name="limit">The maximum number of exception records to return. Defaults to 50.</param>
    /// <returns>A list of <see cref="AppInsightsObject"/> records representing the retrieved exceptions.</returns>
    Task<List<AppInsightsObject>> GetExceptions(int limit = 50);
}
