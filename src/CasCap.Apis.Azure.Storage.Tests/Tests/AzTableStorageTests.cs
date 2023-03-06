using CasCap.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace CasCap.Apis.AzStorage.Tests;

public class AzTableStorageTests : TestBase
{
    public AzTableStorageTests(ITestOutputHelper output) : base(output) { }

#pragma warning disable CS0618 // Type or member is obsolete
    [Fact, Trait("Category", "Azure")]
    public void AzureTableKeys()
    {
        var dtA = DateTime.UtcNow;
        var str = dtA.GetRowKeyOLD();
        var dtB = str.GetPartitionKeyDateTimeOLD();
        Assert.True(dtA == dtB);

        var PartitionKey = "2519150111999999999";//17/02/2017
        var RowKey = "2519149413536369999";//17/02/2017 19:24:06
        var dt1 = PartitionKey.GetPartitionKeyDateTimeOLD();
        var dt2 = RowKey.GetPartitionKeyDateTimeOLD();

        Assert.True(dt1 > new DateTime(1900, 1, 1));
    }
#pragma warning restore CS0618 // Type or member is obsolete
}