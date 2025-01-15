namespace CasCap.Abstractions;

public interface IEventHubPublisherService<T>// where T : IEventHubEvent
{
    Task Push(T obj);
    Task Push(byte[] bytes);
    Task Push(List<T> objs);
    Task Push(List<byte[]> bytesCollection);
    Task SendTestMessages(int numMessagesToSend = 10);
}
