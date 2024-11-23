namespace CasCap.Tests;

public class AzQueueStorageTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task AzQueue()
    {
        string inputTestString = nameof(inputTestString);

        var testObj = new TestMessage { TestString = inputTestString };

        var result1 = await _queueSvc.Enqueue(testObj);
        Assert.True(result1);

        var result2 = await _queueSvc.Enqueue(testObj);
        Assert.True(result2);

        var result3 = await _queueSvc.DequeueSingle<TestMessage>();
        Assert.NotNull(result3.obj);
        Assert.Equal(result3.obj.TestString, inputTestString);

        var result4 = await _queueSvc.Enqueue(testObj);
        Assert.True(result4);

        var result5 = await _queueSvc.DequeueMany<TestMessage>();
        Assert.NotNull(result5);
        Assert.True(result5.Count > 1);
    }
}

public class TestMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Dt { get; set; } = DateTime.UtcNow;
    public string? TestString { get; set; }
}
