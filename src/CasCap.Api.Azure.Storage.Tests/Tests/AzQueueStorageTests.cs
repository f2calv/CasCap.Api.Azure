namespace CasCap.Tests;

public class AzQueueStorageTests(/*ITestOutputHelper output*/) : TestBase/*(output)*/
{
    [Fact]
    public async Task AzQueue()
    {
        string inputTestString = nameof(inputTestString);

        var testObj = new TestMessage { TestString = inputTestString };

        //queue test message #1
        var result1 = await _queueSvc.Enqueue(testObj);
        Assert.True(result1);

        //queue test message #2
        var result2 = await _queueSvc.Enqueue(testObj);
        Assert.True(result2);

        //dequeue test message #1
        var result3 = await _queueSvc.DequeueSingle<TestMessage>();
        Assert.NotNull(result3.obj);
        Assert.Equal(result3.obj.TestString, inputTestString);

        //queue test message #3
        var result4 = await _queueSvc.Enqueue(testObj);
        Assert.True(result4);

        //dequeue test messages #2 & #3
        var result5 = await _queueSvc.DequeueMany<TestMessage>();
        Assert.NotNull(result5);
        Assert.True(result5.Count >= 2, $"actually found {result5.Count} message(s)");
    }
}

public class TestMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Dt { get; set; } = DateTime.UtcNow;
    public string? TestString { get; set; }
}
