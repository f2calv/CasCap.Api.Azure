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
        IOptions<LogAnalyticsOptions> logAnalyticsOptions
        )
    {
        _logger = logger;
        _logAnalyticsOptions = logAnalyticsOptions.Value;
        _client = Auth();
    }

    private static LogsQueryClient Auth()
    {
        //TODO: need to enable managed identity here or use environment variables...
        //https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme?view=azure-dotnet
        return new LogsQueryClient(new DefaultAzureCredential());
        //return new LogsQueryClient(new EnvironmentCredential());
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

    public async Task<List<aiObject>> GetExceptions(int limit = 50)
    {
        var query = $"exceptions | limit {limit} | order by timestamp";
        var queryResults = await _client.QueryWorkspaceAsync(_logAnalyticsOptions.WorkspaceId, query, new QueryTimeRange(TimeSpan.FromDays(1)));
        var l = new List<aiObject>(queryResults.Value.Table.Rows.Count);
        foreach (var e in queryResults.Value.Table.Rows)
        {
            var obj = new aiObject
            {
                timestamp = DateTime.Parse(e[nameof(aiObject.timestamp)].ToString()!),
                cloud_RoleInstance = e[nameof(aiObject.cloud_RoleInstance)].ToString()!,
                customDimensions = e[nameof(aiObject.customDimensions)],
                appId = new Guid(e[nameof(aiObject.appId)].ToString()!),
                iKey = new Guid(e[nameof(aiObject.iKey)].ToString()!),
                problemId = e[nameof(aiObject.problemId)].ToString()!,
                message = e[nameof(aiObject.message)].ToString()!,
                outerMessage = e[nameof(aiObject.outerMessage)].ToString()!,
                innermostMessage = e[nameof(aiObject.innermostMessage)].ToString()!,
                method = e[nameof(aiObject.method)].ToString()!,
                assembly = e[nameof(aiObject.assembly)].ToString()!,
            };
            l.Add(obj);
        }
        return l;
    }
}

public class aiObject
{
    //timestamp, problemId, handledAt, type, message, assembly, method, outerType, outerMessage, outerAssembly, outerMethod, innermostType, innermostMessage, innermostAssembly, innermostMethod, severityLevel, details, itemType, customDimensions, customMeasurements, operation_Name, operation_Id, operation_ParentId, operation_SyntheticSource, session_Id, user_Id, user_AuthenticatedId, user_AccountId, application_Version, client_Type, client_Model, client_OS, client_IP, client_City, client_StateOrProvince, client_CountryOrRegion, client_Browser, cloud_RoleName, cloud_RoleInstance, appId, appName, iKey, sdkVersion, itemId, itemCount
    //https://stackoverflow.com/questions/2380467/c-dynamic-parse-from-system-type
    //public Type? type { get; set; }
    public DateTime? timestamp { get; set; }
    public required string cloud_RoleInstance { get; set; }
    //public string timestamp { get; set; }
    //public Exception exception { get; set; }
    public required object customDimensions { get; set; }
    public required string method { get; set; }
    public required string assembly { get; set; }
    public required string message { get; set; }
    public required string outerMessage { get; set; }
    public required string innermostMessage { get; set; }
    public required string problemId { get; set; }
    public Guid appId { get; set; }
    public Guid iKey { get; set; }
}
