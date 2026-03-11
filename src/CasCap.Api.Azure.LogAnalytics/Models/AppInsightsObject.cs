namespace CasCap.Models;

/// <summary>Represents a single Application Insights exception row returned by a Log Analytics query.</summary>
/// <remarks>
/// Property names intentionally match the Log Analytics column names so that <see langword="nameof"/> can be used as the lookup key.
/// See <see href="https://stackoverflow.com/questions/2380467/c-dynamic-parse-from-system-type" />.
/// </remarks>
public record AppInsightsObject
{
    /// <summary>Gets or sets the timestamp of the exception.</summary>
    public DateTime? timestamp { get; set; }

    /// <summary>Gets or sets the cloud role instance that produced the exception.</summary>
    public required string cloud_RoleInstance { get; set; }

    /// <summary>Gets or sets the custom dimensions associated with the exception.</summary>
    public required object customDimensions { get; set; }

    /// <summary>Gets or sets the method in which the exception was thrown.</summary>
    public required string method { get; set; }

    /// <summary>Gets or sets the assembly in which the exception was thrown.</summary>
    public required string assembly { get; set; }

    /// <summary>Gets or sets the exception message.</summary>
    public required string message { get; set; }

    /// <summary>Gets or sets the outer exception message.</summary>
    public required string outerMessage { get; set; }

    /// <summary>Gets or sets the innermost exception message.</summary>
    public required string innermostMessage { get; set; }

    /// <summary>Gets or sets the problem ID of the exception.</summary>
    public required string problemId { get; set; }

    /// <summary>Gets or sets the Application Insights application ID.</summary>
    public Guid appId { get; set; }

    /// <summary>Gets or sets the instrumentation key.</summary>
    public Guid iKey { get; set; }
}
