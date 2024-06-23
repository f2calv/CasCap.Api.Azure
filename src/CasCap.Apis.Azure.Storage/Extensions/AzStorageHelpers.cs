using System.Globalization;
using System.Text.RegularExpressions;
namespace CasCap.Common.Extensions;

public static class AzStorageHelpers
{
    static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(AzStorageHelpers));

    public static async Task<bool> ExistsAsync(this TableServiceClient client, string tableName)
    {
        await foreach (var tbl in client.QueryAsync(t => t.Name == tableName))
            return true;
        return false;
    }

    //static readonly Regex DisallowedCharsInTableKeys = new(@"[\\\\#%+/?\u0000-\u001F\u007F-\u009F]", RegexOptions.Compiled);
    static readonly Regex DisallowedCharsInTableKeys = new(@"[\\\\#%/?\^\u0000-\u001F\u007F-\u009F]", RegexOptions.Compiled);
    //static readonly Regex DisallowedCharsInTableKeys = new("[#]", RegexOptions.Compiled);
    //https://stackoverflow.com/questions/11514707/azure-table-storage-rowkey-restricted-character-patterns
    public static bool IsKeyValid(this string tableKey)
    {
        return !DisallowedCharsInTableKeys.IsMatch(tableKey);
        //string sanitizedKey = DisallowedCharsInTableKeys.Replace(tableKey, disallowedCharReplacement);
    }

    public static DateTime GetDateFromFileName(this string path, DateTimeKind kind = DateTimeKind.Utc)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        //someDirectory/2016-05-17-some-suffix.log.gz -> 2016-05-17
        var strDt = fileName.Substring(0, 10);
        if (DateTime.TryParse(strDt, out var date))
            return DateTime.SpecifyKind(date, kind);
        else
            throw new ArgumentException("unable to parse {path} to retrieve date", path);
    }

    public static readonly DateTime newMaxDate = new(2050, 1, 1);

    public const string yyMMdd = nameof(yyMMdd);

    /// <summary>
    /// Returns a partition key suitable for Azure Table Storage in the default format 'yyMMdd'.
    /// </summary>
    /// <param name="thisDate"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string GetPartitionKey(this DateTime thisDate, string format = yyMMdd)
    {
        if (thisDate.Year < 2000) throw new ArgumentException("partitionKey only supports > year 2000");
        return thisDate.ToString(format);
    }

    public static DateTime GetDateFromPartitionKey(this string thisPartitionKey, string format = yyMMdd)
        => DateTime.ParseExact(thisPartitionKey, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    const long ticksInADay = 863999999999;

    /// <summary>
    /// This method returns only the Time portion of a DateTime with or without dictionary/lexicographical order.
    ///
    /// Why?
    ///
    /// When storing huge quantities of data with tick-level accuracy the storage space required for every field
    /// is both large in terms of cost and crucially data retrieval speed is slow (less data = faster!).
    ///
    /// Best practise is to split the DateTime up into two parts, storing the Date in either the PartitionKey or event
    /// the table name and the (hopefully unique!) Time portion in the RowKey.
    ///
    /// Plus now if you use lexicographical order you are able to retrieve *only* the TOP n records from the table.
    /// </summary>
    /// <param name="thisDate"></param>
    /// <param name="lexicalOrder"></param>
    /// <returns></returns>
    public static string GetRowKey(this DateTime thisDate, bool lexicalOrder = true)
    {
        var dayTicks = new DateTime(thisDate.Year, thisDate.Month, thisDate.Day).Ticks;
        var todayTicks = thisDate.Ticks - dayTicks;
        if (lexicalOrder) todayTicks = ticksInADay - todayTicks;
        var output = todayTicks.ToString("d12");
        return output;
    }

    public static DateTime GetDateTimeFromRowKey(this string thisRowKey, string PartitionKey, bool lexicalOrder = true)
    {
        thisRowKey = thisRowKey ?? throw new ArgumentNullException(nameof(thisRowKey));
        PartitionKey = PartitionKey ?? throw new ArgumentNullException(nameof(PartitionKey));
        var dt = PartitionKey.GetDateFromPartitionKey();
        return GetDateTimeFromRowKey(thisRowKey, dt, lexicalOrder);
    }

    public static DateTime GetDateTimeFromRowKey(this string thisRowKey, DateTime dt, bool lexicalOrder = true)
    {
        thisRowKey = thisRowKey ?? throw new ArgumentNullException(nameof(thisRowKey));
        if (!long.TryParse(thisRowKey, out long rowKeyValue))
            throw new ArgumentException("unable to parse {rowKey} to retrieve tick count", thisRowKey);
        return GetDateTimeFromRowKey(rowKeyValue, dt, lexicalOrder);
    }

    static DateTime GetDateTimeFromRowKey(long rowKeyValue, DateTime dt, bool lexicalOrder = true)
    {
        var ticks = lexicalOrder ? ticksInADay - rowKeyValue : rowKeyValue;
        return DateTime.SpecifyKind(dt.AddTicks(ticks), DateTimeKind.Utc);
    }
}
