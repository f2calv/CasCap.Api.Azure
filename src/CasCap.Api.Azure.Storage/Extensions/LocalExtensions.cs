namespace CasCap.Common.Extensions;

/// <summary>Extension methods for Azure Table Storage key and date helpers.</summary>
public static class LocalExtensions
{
    /// <summary>Returns <see langword="true"/> if a table with the given name exists in the <see cref="Azure.Data.Tables.TableServiceClient"/>.</summary>
    public static async Task<bool> ExistsAsync(this TableServiceClient client, string tableName)
    {
        await foreach (var tbl in client.QueryAsync(t => t.Name == tableName))
            return true;
        return false;
    }

    private static readonly Regex DisallowedCharsInTableKeys = new(@"[\\\\#%/?\^\u0000-\u001F\u007F-\u009F]", RegexOptions.Compiled);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="tableKey"/> contains only characters permitted by Azure Table Storage.
    /// </summary>
    /// <remarks>See <see href="https://stackoverflow.com/questions/11514707/azure-table-storage-rowkey-restricted-character-patterns" />.</remarks>
    public static bool IsKeyValid(this string tableKey)
        => !DisallowedCharsInTableKeys.IsMatch(tableKey);

    /// <summary>Extracts a <see cref="DateTime"/> from a file path whose name starts with a date in <c>yyyy-MM-dd</c> format.</summary>
    public static DateTime GetDateFromFileName(this string path, DateTimeKind kind = DateTimeKind.Utc)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        var strDt = fileName.Substring(0, 10);
        if (DateTime.TryParse(strDt, out var date))
            return DateTime.SpecifyKind(date, kind);
        else
            throw new ArgumentException("unable to parse {path} to retrieve date", path);
    }

    /// <summary>A sentinel maximum date value (2050-01-01 UTC) used to represent "no expiry".</summary>
    public static readonly DateTime newMaxDate = new(2050, 1, 1);

    /// <summary>The default date format string used for partition keys (<c>yyMMdd</c>).</summary>
    public const string yyMMdd = nameof(yyMMdd);

    /// <summary>
    /// Returns a partition key suitable for Azure Table Storage in the default format <c>yyMMdd</c>.
    /// </summary>
    /// <param name="thisDate">The date to convert.</param>
    /// <param name="format">The date format string; defaults to <see cref="yyMMdd"/>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="thisDate"/> is before the year 2000.</exception>
    public static string GetPartitionKey(this DateTime thisDate, string format = yyMMdd)
    {
        if (thisDate.Year < 2000) throw new ArgumentException("partitionKey only supports > year 2000");
        return thisDate.ToString(format);
    }

    /// <summary>Parses a partition key string back into a <see cref="DateTime"/> using the given <paramref name="format"/>.</summary>
    public static DateTime GetDateFromPartitionKey(this string thisPartitionKey, string format = yyMMdd)
        => DateTime.ParseExact(thisPartitionKey, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    private const long ticksInADay = 863999999999;

    /// <summary>
    /// Returns only the time-of-day portion of <paramref name="thisDate"/> as a 12-digit string row key.
    /// </summary>
    /// <param name="thisDate">The source date/time.</param>
    /// <param name="lexicalOrder">
    /// When <see langword="true"/> (default) the tick value is stored in descending (lexicographically newest-first) order,
    /// allowing the TOP n most-recent records to be retrieved efficiently.
    /// </param>
    public static string GetRowKey(this DateTime thisDate, bool lexicalOrder = true)
    {
        var dayTicks = new DateTime(thisDate.Year, thisDate.Month, thisDate.Day).Ticks;
        var todayTicks = thisDate.Ticks - dayTicks;
        if (lexicalOrder) todayTicks = ticksInADay - todayTicks;
        var output = todayTicks.ToString("d12");
        return output;
    }

    /// <summary>Reconstructs a <see cref="DateTime"/> from a row key and partition key string.</summary>
    public static DateTime GetDateTimeFromRowKey(this string thisRowKey, string PartitionKey, bool lexicalOrder = true)
    {
        thisRowKey = thisRowKey ?? throw new ArgumentNullException(nameof(thisRowKey));
        PartitionKey = PartitionKey ?? throw new ArgumentNullException(nameof(PartitionKey));
        var dt = PartitionKey.GetDateFromPartitionKey();
        return GetDateTimeFromRowKey(thisRowKey, dt, lexicalOrder);
    }

    /// <summary>Reconstructs a <see cref="DateTime"/> from a row key string and a base <see cref="DateTime"/>.</summary>
    public static DateTime GetDateTimeFromRowKey(this string thisRowKey, DateTime dt, bool lexicalOrder = true)
    {
        thisRowKey = thisRowKey ?? throw new ArgumentNullException(nameof(thisRowKey));
        if (!long.TryParse(thisRowKey, out long rowKeyValue))
            throw new ArgumentException("unable to parse {rowKey} to retrieve tick count", thisRowKey);
        return GetDateTimeFromRowKey(rowKeyValue, dt, lexicalOrder);
    }

    private static DateTime GetDateTimeFromRowKey(long rowKeyValue, DateTime dt, bool lexicalOrder = true)
    {
        var ticks = lexicalOrder ? ticksInADay - rowKeyValue : rowKeyValue;
        return DateTime.SpecifyKind(dt.AddTicks(ticks), DateTimeKind.Utc);
    }
}
