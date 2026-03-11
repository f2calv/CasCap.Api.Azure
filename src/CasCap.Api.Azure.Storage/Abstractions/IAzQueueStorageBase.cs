namespace CasCap.Abstractions;

/// <summary>
/// Defines the contract for interacting with an Azure Queue Storage queue.
/// </summary>
public interface IAzQueueStorageBase
{
    /// <summary>
    /// Dequeues and deserializes a single message from the queue.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the message body into.</typeparam>
    /// <returns>
    /// A tuple containing the deserialized object (or <see langword="null"/> if deserialization failed)
    /// and the raw <see cref="QueueMessage"/> that was dequeued.
    /// </returns>
    Task<(T? obj, QueueMessage message)> DequeueSingle<T>() where T : class;

    /// <summary>
    /// Dequeues and deserializes multiple messages from the queue.
    /// </summary>
    /// <typeparam name="T">The type to deserialize each message body into.</typeparam>
    /// <param name="limit">The maximum number of messages to dequeue. Defaults to 1.</param>
    /// <returns>A list of deserialized objects retrieved from the queue.</returns>
    Task<List<T>> DequeueMany<T>(int limit = 1) where T : class;

    /// <summary>
    /// Serializes and enqueues a single object onto the queue.
    /// </summary>
    /// <typeparam name="T">The type of the object to enqueue.</typeparam>
    /// <param name="obj">The object to serialize and add to the queue.</param>
    /// <returns><see langword="true"/> if the message was successfully enqueued; otherwise, <see langword="false"/>.</returns>
    Task<bool> Enqueue<T>(T obj) where T : class;

    /// <summary>
    /// Serializes and enqueues a collection of objects onto the queue.
    /// </summary>
    /// <typeparam name="T">The type of the objects to enqueue.</typeparam>
    /// <param name="objs">The list of objects to serialize and add to the queue.</param>
    /// <returns><see langword="true"/> if at least one message was successfully enqueued; otherwise, <see langword="false"/>.</returns>
    Task<bool> Enqueue<T>(List<T> objs) where T : class;
}
