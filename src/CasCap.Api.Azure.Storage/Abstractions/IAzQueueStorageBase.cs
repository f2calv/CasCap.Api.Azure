namespace CasCap.Abstractions;

/// <summary>Base abstraction for Azure Queue Storage operations.</summary>
public interface IAzQueueStorageBase
{
    /// <summary>Dequeues and deserializes a single message of type <typeparamref name="T"/>, returning the object and the raw <see cref="Azure.Storage.Queues.Models.QueueMessage"/>.</summary>
    Task<(T? obj, QueueMessage message)> DequeueSingle<T>() where T : class;

    /// <summary>Dequeues and deserializes up to <paramref name="limit"/> messages of type <typeparamref name="T"/>.</summary>
    Task<List<T>> DequeueMany<T>(int limit = 1) where T : class;

    /// <summary>Serializes and enqueues a single object of type <typeparamref name="T"/>.</summary>
    Task<bool> Enqueue<T>(T obj) where T : class;

    /// <summary>Serializes and enqueues a list of objects of type <typeparamref name="T"/>.</summary>
    Task<bool> Enqueue<T>(List<T> objs) where T : class;
}
