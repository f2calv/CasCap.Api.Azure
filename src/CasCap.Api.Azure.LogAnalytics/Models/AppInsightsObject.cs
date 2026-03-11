namespace CasCap.Models;

/// <summary>
/// Represents a single exception record returned from an Application Insights Log Analytics query.
/// </summary>
/// <remarks>
/// Property names intentionally match the Application Insights exception table column names
/// returned by the Log Analytics query API, which use lowercase and snake_case conventions.
/// </remarks>
public record AppInsightsObject
{
    //timestamp, problemId, handledAt, type, message, assembly, method, outerType, outerMessage, outerAssembly, outerMethod, innermostType, innermostMessage, innermostAssembly, innermostMethod, severityLevel, details, itemType, customDimensions, customMeasurements, operation_Name, operation_Id, operation_ParentId, operation_SyntheticSource, session_Id, user_Id, user_AuthenticatedId, user_AccountId, application_Version, client_Type, client_Model, client_OS, client_IP, client_City, client_StateOrProvince, client_CountryOrRegion, client_Browser, cloud_RoleName, cloud_RoleInstance, appId, appName, iKey, sdkVersion, itemId, itemCount
    //https://stackoverflow.com/questions/2380467/c-dynamic-parse-from-system-type
    //public Type? type { get; set; }
    /// <summary>Gets or sets the UTC timestamp when the exception was recorded.</summary>
    public DateTime? timestamp { get; set; }
    /// <summary>Gets or sets the name of the cloud role instance (server/host) where the exception occurred.</summary>
    public required string cloud_RoleInstance { get; set; }
    //public string timestamp { get; set; }
    //public Exception exception { get; set; }
    /// <summary>Gets or sets the custom dimensions associated with the exception telemetry item.</summary>
    public required object customDimensions { get; set; }
    /// <summary>Gets or sets the name of the method where the exception was thrown.</summary>
    public required string method { get; set; }
    /// <summary>Gets or sets the assembly in which the exception originated.</summary>
    public required string assembly { get; set; }
    /// <summary>Gets or sets the exception message.</summary>
    public required string message { get; set; }
    /// <summary>Gets or sets the message of the outer (wrapping) exception, if any.</summary>
    public required string outerMessage { get; set; }
    /// <summary>Gets or sets the message of the innermost exception in the exception chain.</summary>
    public required string innermostMessage { get; set; }
    /// <summary>Gets or sets the problem identifier used to group related exceptions.</summary>
    public required string problemId { get; set; }
    /// <summary>Gets or sets the Application Insights application ID.</summary>
    public Guid appId { get; set; }
    /// <summary>Gets or sets the Application Insights instrumentation key.</summary>
    public Guid iKey { get; set; }
}
