namespace CasCap.Services;

//https://gist.github.com/alexeldeib/7bfa6e671904cd33aaaac5c3d3ff8e09
//https://dev.applicationinsights.io/documentation/Authorization/AAD-Application-Setup
//https://stackoverflow.com/questions/62898365/azure-app-insights-api-to-get-traces-using-query-in-c-sharp
//https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-configure-app-access-web-apis#add-credentials-to-your-web-application
//https://zimmergren.net/retrieve-logs-from-application-insights-programmatically-with-net-core-c/
//above all deprecated info...
//https://learn.microsoft.com/en-us/dotnet/api/overview/azure/monitor.query-readme?view=azure-dotnet
public class LogAnalyticsQueryService : ILogAnalyticsQueryService
{
    private readonly ILogger _logger;
    private readonly LogAnalyticsOptions _logAnalyticsOptions;

    private readonly LogsQueryClient _client;

    public LogAnalyticsQueryService(ILogger<LogAnalyticsQueryService> logger,
        IOptions<LogAnalyticsOptions> logAnalyticsOptions,
        TokenCredential credential
        )
    {
        _logger = logger;
        _logAnalyticsOptions = logAnalyticsOptions.Value;
        _client = new LogsQueryClient(credential);
    }

    public async Task Query(QueryTimeRange timeRange)
    {
        //var query = "traces | where operation_Id contains '33f491236bb412419002b006e1c3058b'";
        //var query = "exceptions | order by timestamp";
        //var query = "union * | limit 5";
        var query = "union * | limit 50 | order by timestamp";
        //var query = "availabilityResults | summarize count() by name, bin(duration,500) | order by _count desc";
        //var metric = "availabilityResults/duration";

        var queryResults = await _client.QueryWorkspaceAsync(_logAnalyticsOptions.WorkspaceId, query, timeRange);
        //var queryResults = await _client.Query.ExecuteAsync(_appInsightsOptions.ApiApplicationId, query, timespan);
        foreach (var row in queryResults.Value.Table.Rows)
        {
            // Do something with query results
            Console.WriteLine(string.Join("    ", row));
        }
    }

    //public async Task GetCustomEvents(string timespan = "P1D")
    //{
    //    var query = "customEvents";
    //    var queryResults = await _client.Query.ExecuteAsync(_appInsightsOptions.ApiApplicationId, query, timespan);
    //    //foreach (var e in queryResults.Results)
    //    //{
    //    //    var name = e.Values[0]CustomEvent.Name;
    //    //    var time = e.Timestamp?.ToString("s") ?? "";
    //    //    Console.WriteLine($"{time}: {name}");
    //    //}
    //}

    public async Task<List<AppInsightsObject>> GetExceptions(int limit = 50)
    {
        var query = $"exceptions | limit {limit} | order by timestamp";
        var queryResults = await _client.QueryWorkspaceAsync(_logAnalyticsOptions.WorkspaceId, query, new QueryTimeRange(TimeSpan.FromDays(1)));
        var l = new List<AppInsightsObject>(queryResults.Value.Table.Rows.Count);
        foreach (var e in queryResults.Value.Table.Rows)
        {
            var obj = new AppInsightsObject
            {
                timestamp = DateTime.Parse(e[nameof(AppInsightsObject.timestamp)].ToString()!),
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
