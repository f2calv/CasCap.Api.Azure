namespace CasCap.Services;

/// <inheritdoc/>
/// <remarks>
/// See <see href="https://gist.github.com/alexeldeib/7bfa6e671904cd33aaaac5c3d3ff8e09" />,
/// <see href="https://zimmergren.net/retrieve-logs-from-application-insights-programmatically-with-net-core-c/" />,
/// and <see href="https://learn.microsoft.com/en-us/dotnet/api/overview/azure/monitor.query-readme?view=azure-dotnet" />.
/// </remarks>
public sealed class QueryService(
    ILogger<QueryService> logger,
    IOptions<LogAnalyticsConfig> logAnalyticsConfig,
    TokenCredential credential) : IQueryService
{
    private readonly LogsQueryClient client = new(credential);

    /// <inheritdoc/>
    public async Task Query(QueryTimeRange timeRange)
    {
        var query = "union * | limit 50 | order by timestamp";

        var queryResults = await client.QueryWorkspaceAsync(logAnalyticsConfig.Value.WorkspaceId, query, timeRange).ConfigureAwait(false);
        foreach (var row in queryResults.Value.Table.Rows)
            logger.LogInformation("{ClassName} {Row}", nameof(QueryService), string.Join("    ", row));
    }

    /// <inheritdoc/>
    public async Task<List<AppInsightsObject>> GetExceptions(int limit = 50)
    {
        var query = $"exceptions | limit {limit} | order by timestamp";
        var queryResults = await client.QueryWorkspaceAsync(logAnalyticsConfig.Value.WorkspaceId, query, new QueryTimeRange(TimeSpan.FromDays(1))).ConfigureAwait(false);
        var l = new List<AppInsightsObject>(queryResults.Value.Table.Rows.Count);
        foreach (var e in queryResults.Value.Table.Rows)
        {
            var obj = new AppInsightsObject
            {
                timestamp = DateTime.Parse(e[nameof(AppInsightsObject.timestamp)].ToString()!, CultureInfo.InvariantCulture),
                cloud_RoleInstance = e[nameof(AppInsightsObject.cloud_RoleInstance)].ToString()!,
                customDimensions = e[nameof(AppInsightsObject.customDimensions)],
                appId = new Guid(e[nameof(AppInsightsObject.appId)].ToString()!),
                iKey = new Guid(e[nameof(AppInsightsObject.iKey)].ToString()!),
                problemId = e[nameof(AppInsightsObject.problemId)].ToString()!,
                message = e[nameof(AppInsightsObject.message)].ToString()!,
                outerMessage = e[nameof(AppInsightsObject.outerMessage)].ToString()!,
                innermostMessage = e[nameof(AppInsightsObject.innermostMessage)].ToString()!,
                method = e[nameof(AppInsightsObject.method)].ToString()!,
                assembly = e[nameof(AppInsightsObject.assembly)].ToString()!,
            };
            l.Add(obj);
        }
        return l;
    }
}
