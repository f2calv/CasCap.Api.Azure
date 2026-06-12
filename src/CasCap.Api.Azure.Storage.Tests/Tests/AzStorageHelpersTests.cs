namespace CasCap.Tests;

/// <summary>Unit tests for <see cref="CasCap.Common.Extensions.StorageExtensions"/> storage key helpers.</summary>
public class AzStorageHelpersTests
{
    [Fact, Trait("Category", "Storage Keys")]
    public void GetPartitionKey()
    {
        var date = new DateTime(2023, 6, 8, 4, 2, 9, DateTimeKind.Utc);
        Assert.Equal("230608", date.GetPartitionKey());
    }

    [Fact, Trait("Category", "Storage Keys")]
    public void GetPartitionKeyRejectsPreMillennium()
    {
        var date = new DateTime(1999, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        Assert.Throws<ArgumentException>(() => date.GetPartitionKey());
    }

    [Theory, Trait("Category", "Storage Keys")]
    [InlineData("230608")]
    [InlineData("000101")]
    [InlineData("491231")]
    public void GetDateFromPartitionKeyRoundTrip(string partitionKey)
    {
        var date = partitionKey.GetDateFromPartitionKey();
        Assert.Equal(partitionKey, date.GetPartitionKey());
        Assert.Equal(DateTimeKind.Utc, date.Kind);
    }

    [Theory, Trait("Category", "Storage Keys")]
    [InlineData(true)]
    [InlineData(false)]
    public void GetRowKeyRoundTrip(bool lexicalOrder)
    {
        var original = new DateTime(2023, 6, 8, 4, 2, 9, 783, DateTimeKind.Utc).AddTicks(2039);

        var partitionKey = original.GetPartitionKey();
        var rowKey = original.GetRowKey(lexicalOrder);

        var reconstructed = rowKey.GetDateTimeFromRowKey(partitionKey, lexicalOrder);

        Assert.Equal(original, reconstructed);
        Assert.Equal(DateTimeKind.Utc, reconstructed.Kind);
    }

    [Fact, Trait("Category", "Storage Keys")]
    public void GetRowKeyIsTwelveDigits()
    {
        var rowKey = new DateTime(2023, 6, 8, 4, 2, 9, DateTimeKind.Utc).GetRowKey();
        Assert.Equal(12, rowKey.Length);
        Assert.True(long.TryParse(rowKey, out _));
    }

    [Fact, Trait("Category", "Storage Keys")]
    public void GetRowKeyLexicalOrderSortsNewestFirst()
    {
        var date = new DateTime(2023, 6, 8, 0, 0, 0, DateTimeKind.Utc);
        var earlier = date.AddHours(1).GetRowKey();
        var later = date.AddHours(2).GetRowKey();

        // With lexical inversion, the later timestamp produces the smaller (earlier-sorting) row key.
        Assert.True(string.CompareOrdinal(later, earlier) < 0);
    }

    [Fact, Trait("Category", "Storage Keys")]
    public void GetDateTimeFromRowKeyRejectsNonNumeric()
    {
        var partitionKey = "230608";
        Assert.Throws<ArgumentException>(() => "not-a-number".GetDateTimeFromRowKey(partitionKey));
    }

    [Theory, Trait("Category", "Storage Keys")]
    [InlineData("validkey123", true)]
    [InlineData("EURUSD", true)]
    [InlineData("abc#def", false)]
    [InlineData("abc/def", false)]
    [InlineData("abc\\def", false)]
    [InlineData("abc?def", false)]
    public void IsKeyValid(string key, bool expected)
    {
        Assert.Equal(expected, key.IsKeyValid());
    }

    [Fact, Trait("Category", "Storage Keys")]
    public void GetDateFromFileName()
    {
        var date = "2016-05-17-some-suffix.log.gz".GetDateFromFileName();
        Assert.Equal(new DateTime(2016, 5, 17, 0, 0, 0, DateTimeKind.Utc), date);
        Assert.Equal(DateTimeKind.Utc, date.Kind);
    }

    [Fact, Trait("Category", "Storage Keys")]
    public void GetDateFromFileNameRejectsUnparseable()
    {
        Assert.Throws<ArgumentException>(() => "not-a-date-prefix.log".GetDateFromFileName());
    }
}
