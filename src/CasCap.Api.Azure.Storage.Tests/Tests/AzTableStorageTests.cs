namespace CasCap.Tests;

public class AzTableStorageTests(/*ITestOutputHelper output*/) : TestBase/*(output)*/
{
    private static readonly string _tableName = $"fruits{Environment.Version.Major}";

    [Fact]
    public async Task AzTableApples()
    {
        using var cts = new CancellationTokenSource();

        var apples = new List<Apple>
        {
            new() { PartitionKey = "apple", RowKey = Guid.NewGuid().ToString(), Variety = "Granny Smith", WeightGrams = 182.5, Colour = "Green" },
            new() { PartitionKey = "apple", RowKey = Guid.NewGuid().ToString(), Variety = "Fuji", WeightGrams = 205.0, Colour = "Red" },
            new() { PartitionKey = "apple", RowKey = Guid.NewGuid().ToString(), Variety = "Gala", WeightGrams = 168.3, Colour = "Red" },
        };

        var failed = await _tableSvc.UploadData(_tableName, apples, cancellationToken: cts.Token);
        Assert.Empty(failed);
    }

    [Fact]
    public async Task AzTableOranges()
    {
        using var cts = new CancellationTokenSource();

        var oranges = new List<Orange>
        {
            new() { PartitionKey = "orange", RowKey = Guid.NewGuid().ToString(), Type = "Navel", WeightGrams = 215.0, Colour = "Orange" },
            new() { PartitionKey = "orange", RowKey = Guid.NewGuid().ToString(), Type = "Blood", WeightGrams = 195.5, Colour = "Red-Orange" },
            new() { PartitionKey = "orange", RowKey = Guid.NewGuid().ToString(), Type = "Clementine", WeightGrams = 98.2, Colour = "Orange" },
        };

        var failed = await _tableSvc.UploadData(_tableName, oranges, cancellationToken: cts.Token);
        Assert.Empty(failed);
    }
}

public class Apple : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string? Variety { get; set; }
    public double WeightGrams { get; set; }
    public string? Colour { get; set; }
}

public class Orange : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string? Type { get; set; }
    public double WeightGrams { get; set; }
    public string? Colour { get; set; }
}
