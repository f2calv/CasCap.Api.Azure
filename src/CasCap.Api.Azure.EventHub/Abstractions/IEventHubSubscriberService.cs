namespace CasCap.Abstractions;

/// <summary>
/// Defines the contract for subscribing to and processing events from an Azure Event Hub.
/// </summary>
/// <typeparam name="T">The event type to receive and deserialize from the Event Hub.</typeparam>
public interface IEventHubSubscriberService<T>
{
    /// <summary>
    /// Starts receiving and processing events from the Event Hub until cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that signals when processing should stop.</param>
    Task InitiateReceive(CancellationToken cancellationToken);
}
