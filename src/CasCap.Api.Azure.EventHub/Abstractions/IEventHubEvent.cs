namespace CasCap.Abstractions;

/// <summary>
/// Marker interface for events published to or received from an Azure Event Hub.
/// Implement this interface on event payload types to indicate they are intended for Event Hub messaging.
/// </summary>
public interface IEventHubEvent
{
}
