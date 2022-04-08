using Microsoft.Extensions.Logging;
using Xunit;
namespace CasCap.Tests;

public class AzQueueStorageTests
{
    [Fact]
    public async Task AzQueue()
    {
        string inputTestString = nameof(inputTestString);
        var loggerFactory = new LoggerFactory();
        ApplicationLogging.LoggerFactory = loggerFactory;//is this still needed?
                                                         //should be AzQueueServiceBase but its marked abstract...
        IAzQueueService svc = new AzQueueService(loggerFactory.CreateLogger<AzQueueService>());

        var testObj = new TestMessage { testString = inputTestString };

        var result1 = await svc.Enqueue(testObj);
        Assert.True(result1);

        var result2 = await svc.Enqueue(testObj);
        Assert.True(result2);

        var result3 = await svc.DequeueSingle<TestMessage>();
        Assert.NotNull(result3.obj);
        Assert.Equal(result3.obj.testString, inputTestString);

        var result4 = await svc.Enqueue(testObj);
        Assert.True(result4);

        var result5 = await svc.DequeueMany<TestMessage>();
        Assert.NotNull(result5);
        Assert.True(result5.Count > 1);
    }
}

public class TestMessage
{
    public Guid id { get; set; } = Guid.NewGuid();
    public DateTime dt { get; set; } = DateTime.UtcNow;
    public string testString { get; set; }
}