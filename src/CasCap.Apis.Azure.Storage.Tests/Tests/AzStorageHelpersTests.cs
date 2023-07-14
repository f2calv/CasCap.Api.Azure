using System.Globalization;

namespace CasCap.Tests;

public class AzStorageTests
{
    [Fact, Trait("Category", "Azure")]
    public void PartitionKeyTest()
    {
        var utcNow = DateTime.UtcNow;
        Assert.True(utcNow.ToString(AzStorageHelpers.yyMMdd) == utcNow.GetPartitionKey());

        var sTimestamp = "2023-06-08T04:02:09.7832039Z";
        var timestamp1 = DateTime.Parse(sTimestamp);
        var timestamp2 = DateTime.Parse(sTimestamp).ToUniversalTime();
        var timestamp = DateTime.ParseExact(sTimestamp, "o", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        var utcTimestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);

        var rowKey = "718702354378";
        var partitionKey = utcTimestamp.ToString(AzStorageHelpers.yyMMdd);
        var utcRowKey = rowKey.GetDateTimeFromRowKey(partitionKey);

        var tsDiff = utcTimestamp - utcRowKey;
        Assert.True(true);
    }
}