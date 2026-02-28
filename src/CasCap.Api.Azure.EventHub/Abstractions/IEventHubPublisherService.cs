namespace CasCap.Abstractions;

/// <summary>
/// Defines the contract for publishing strongly-typed events to an Azure Event Hub.
/// </summary>
/// <typeparam name="T">The event type to publish.</typeparam>
public interface IEventHubPublisherService<T>// where T : IEventHubEvent
{
    /// <summary>
    /// Serializes and publishes a single event to the Event Hub.
    /// </summary>
    /// <param name="obj">The event object to publish.</param>
    Task Push(T obj);

    /// <summary>
    /// Publishes raw bytes as a single event to the Event Hub.
    /// </summary>
    /// <param name="bytes">The raw event data to publish.</param>
    Task Push(byte[] bytes);

    /// <summary>
    /// Serializes and publishes a list of events to the Event Hub in a single batch.
    /// </summary>
    /// <param name="objs">The list of event objects to publish.</param>
    Task Push(List<T> objs);

    /// <summary>
    /// Publishes a collection of raw byte arrays as a single batch to the Event Hub.
    /// </summary>
    /// <param name="bytesCollection">The list of raw event data payloads to publish.</param>
    Task Push(List<byte[]> bytesCollection);

    /// <summary>
    /// Sends a configurable number of test messages to the Event Hub.
    /// </summary>
    /// <param name="numMessagesToSend">The number of test messages to send. Defaults to 10.</param>
    Task SendTestMessages(int numMessagesToSend = 10);
}
