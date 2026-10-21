namespace CasCap.Tests;

/// <summary>Integration tests for <see cref="AzQueueStorageBase"/>.</summary>
public class AzQueueStorageTests(/*ITestOutputHelper output*/) : TestBase/*(output)*/
{
    [Fact, Trait("Category", "Integration")]
    public async Task AzQueue()
    {
        string inputTestString = nameof(inputTestString);

        var testObj = new TestMessage { TestString = inputTestString };

        //queue test message #1
        var result1 = await _queueSvc.Enqueue(testObj, TestContext.Current.CancellationToken);
        Assert.True(result1);

        //queue test message #2
        var result2 = await _queueSvc.Enqueue(testObj, TestContext.Current.CancellationToken);
        Assert.True(result2);

        //dequeue test message #1
        var (dequeuedObj, _) = await _queueSvc.DequeueSingle<TestMessage>(TestContext.Current.CancellationToken);
        Assert.NotNull(dequeuedObj);
        Assert.Equal(dequeuedObj.TestString, inputTestString);

        //queue test message #3
        var result4 = await _queueSvc.Enqueue(testObj, TestContext.Current.CancellationToken);
        Assert.True(result4);

        //dequeue test messages #2 & #3
        var result5 = await _queueSvc.DequeueMany<TestMessage>(limit: 10, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result5);
        Assert.True(result5.Count >= 2, $"actually found {result5.Count} message(s)");
    }
}
