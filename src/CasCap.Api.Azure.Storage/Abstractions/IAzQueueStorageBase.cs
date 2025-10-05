namespace CasCap.Abstractions;

public interface IAzQueueStorageBase
{
    Task<(T? obj, QueueMessage message)> DequeueSingle<T>() where T : class;
    Task<List<T>> DequeueMany<T>(int limit = 1) where T : class;
    Task<bool> Enqueue<T>(T obj) where T : class;
    Task<bool> Enqueue<T>(List<T> objs) where T : class;
}
