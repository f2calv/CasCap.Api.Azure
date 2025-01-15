namespace CasCap.Abstractions;

public interface IEventHubSubscriberService<T>
{
    Task InitiateReceive(CancellationToken cancellationToken);
}
