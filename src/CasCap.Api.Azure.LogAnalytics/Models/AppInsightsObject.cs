namespace CasCap.Models;

public record AppInsightsObject
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
