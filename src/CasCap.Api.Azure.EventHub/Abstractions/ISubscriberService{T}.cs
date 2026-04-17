namespace CasCap.Abstractions;

/// <summary>Abstraction for receiving and processing messages from an Azure Event Hub.</summary>
public interface ISubscriberService<T>
{
    /// <summary>Begins processing events from the Event Hub until the <paramref name="cancellationToken"/> is signalled.</summary>
    Task InitiateReceive(CancellationToken cancellationToken);
}
