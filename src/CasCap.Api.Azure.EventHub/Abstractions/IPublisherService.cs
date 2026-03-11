namespace CasCap.Abstractions;

/// <summary>Abstraction for publishing messages to an Azure Event Hub.</summary>
public interface IPublisherService<T>// where T : IEvent
{
    /// <summary>Serializes and pushes a single event object to the Event Hub.</summary>
    Task Push(T obj);

    /// <summary>Pushes a raw byte array as a single event to the Event Hub.</summary>
    Task Push(byte[] bytes);

    /// <summary>Serializes and pushes a list of event objects to the Event Hub.</summary>
    Task Push(List<T> objs);

    /// <summary>Pushes a collection of raw byte arrays as events to the Event Hub.</summary>
    Task Push(List<byte[]> bytesCollection);

    /// <summary>Sends <paramref name="numMessagesToSend"/> test messages to the Event Hub.</summary>
    Task SendTestMessages(int numMessagesToSend = 10);
}
