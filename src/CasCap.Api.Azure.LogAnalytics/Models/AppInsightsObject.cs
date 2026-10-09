namespace CasCap.Models;

/// <summary>Represents a single exception record returned from an Application Insights Log Analytics query.</summary>
/// <remarks>
/// Serialized property names match the Application Insights exception table column names returned
/// by the Log Analytics query API, which use lowercase and snake_case conventions.
/// </remarks>
public record AppInsightsObject
{
    /// <summary>Gets or sets the UTC timestamp when the exception was recorded.</summary>
    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    /// <summary>Gets or sets the name of the cloud role instance (server/host) where the exception occurred.</summary>
    [JsonPropertyName("cloud_RoleInstance")]
    public required string CloudRoleInstance { get; set; }

    /// <summary>Gets or sets the custom dimensions associated with the exception telemetry item.</summary>
    [JsonPropertyName("customDimensions")]
    public required object CustomDimensions { get; set; }

    /// <summary>Gets or sets the name of the method where the exception was thrown.</summary>
    [JsonPropertyName("method")]
    public required string Method { get; set; }

    /// <summary>Gets or sets the assembly in which the exception originated.</summary>
    [JsonPropertyName("assembly")]
    public required string Assembly { get; set; }

    /// <summary>Gets or sets the exception message.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    /// <summary>Gets or sets the message of the outer (wrapping) exception, if any.</summary>
    [JsonPropertyName("outerMessage")]
    public required string OuterMessage { get; set; }

    /// <summary>Gets or sets the message of the innermost exception in the exception chain.</summary>
    [JsonPropertyName("innermostMessage")]
    public required string InnermostMessage { get; set; }

    /// <summary>Gets or sets the problem identifier used to group related exceptions.</summary>
    [JsonPropertyName("problemId")]
    public required string ProblemId { get; set; }

    /// <summary>Gets or sets the Application Insights application ID.</summary>
    [JsonPropertyName("appId")]
    public Guid AppId { get; set; }

    /// <summary>Gets or sets the Application Insights instrumentation key.</summary>
    [JsonPropertyName("iKey")]
    public Guid InstrumentationKey { get; set; }
}
