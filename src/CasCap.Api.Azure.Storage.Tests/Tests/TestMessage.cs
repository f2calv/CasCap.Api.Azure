namespace CasCap.Tests;

/// <summary>Simple test message payload used in queue storage tests.</summary>
public class TestMessage
{
    /// <summary>Gets or sets a unique identifier for this message.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the UTC timestamp at which this message was created.</summary>
    public DateTime Dt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets an arbitrary test string value.</summary>
    public string? TestString { get; set; }
}
