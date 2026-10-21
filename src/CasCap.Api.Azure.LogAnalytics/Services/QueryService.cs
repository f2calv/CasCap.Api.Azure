namespace CasCap.Services;

/// <inheritdoc/>
/// <remarks>
/// See <see href="https://gist.github.com/alexeldeib/7bfa6e671904cd33aaaac5c3d3ff8e09" />,
/// <see href="https://zimmergren.net/retrieve-logs-from-application-insights-programmatically-with-net-core-c/" />,
/// and <see href="https://learn.microsoft.com/en-us/dotnet/api/overview/azure/monitor.query-readme?view=azure-dotnet" />.
/// </remarks>
public sealed partial class QueryService(
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
        {
            if (!logger.IsEnabled(LogLevel.Information))
                continue;

            var formattedRow = string.Join("    ", row);
            LogRow(logger, nameof(QueryService), formattedRow);
        }
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
                Timestamp = DateTime.Parse(e["timestamp"].ToString()!, CultureInfo.InvariantCulture),
                CloudRoleInstance = e["cloud_RoleInstance"].ToString()!,
                CustomDimensions = e["customDimensions"],
                AppId = new Guid(e["appId"].ToString()!),
                InstrumentationKey = new Guid(e["iKey"].ToString()!),
                ProblemId = e["problemId"].ToString()!,
                Message = e["message"].ToString()!,
                OuterMessage = e["outerMessage"].ToString()!,
                InnermostMessage = e["innermostMessage"].ToString()!,
                Method = e["method"].ToString()!,
                Assembly = e["assembly"].ToString()!,
            };
            l.Add(obj);
        }
        return l;
    }

    [LoggerMessage(LogLevel.Information, "{ClassName} {Row}")]
    private static partial void LogRow(ILogger logger, string className, string row);
}
