using Xunit;
using Xunit.Abstractions;

namespace CasCap.Apis.AzStorage.Tests;

public class AzQueueStorageTests : TestBase
{
    public AzQueueStorageTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task End2End()
    {
        var inputTestString = nameof(End2End);

        var testObj = new TestMessage { testString = inputTestString };

        var result1 = await _queueSvc.Enqueue(testObj);
        Assert.True(result1);

        var result2 = await _queueSvc.Enqueue(testObj);
        Assert.True(result2);

        var result3 = await _queueSvc.DequeueSingle<TestMessage>();
        Assert.NotNull(result3.Item1);//todo: add named outputs
        Assert.Equal(result3.Item1.testString, inputTestString);

        var result4 = await _queueSvc.Enqueue(testObj);
        Assert.True(result4);

        var result5 = await _queueSvc.DequeueMany<TestMessage>();
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