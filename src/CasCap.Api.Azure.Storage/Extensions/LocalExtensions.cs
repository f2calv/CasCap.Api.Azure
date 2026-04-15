namespace CasCap.Common.Extensions;

/// <summary>Extension methods for Azure Table Storage key and date helpers.</summary>
public static class LocalExtensions
{
    /// <summary>
    /// Checks whether a table with the specified name exists in the given <see cref="TableServiceClient"/>.
    /// </summary>
    /// <param name="client">The <see cref="TableServiceClient"/> to query.</param>
    /// <param name="tableName">The name of the table to look up.</param>
    /// <returns><see langword="true"/> if the table exists; otherwise, <see langword="false"/>.</returns>
    public static async Task<bool> ExistsAsync(this TableServiceClient client, string tableName)
    {
        await foreach (var tbl in client.QueryAsync(t => t.Name == tableName))
            return true;
        return false;
    }

    private static readonly Regex DisallowedCharsInTableKeys = new(@"[\\\\#%/?\^\u0000-\u001F\u007F-\u009F]", RegexOptions.Compiled);
    //private static readonly Regex DisallowedCharsInTableKeys = new("[#]", RegexOptions.Compiled);
    //https://stackoverflow.com/questions/11514707/azure-table-storage-rowkey-restricted-character-patterns
    /// <summary>
    /// Determines whether the specified string is a valid Azure Table Storage partition or row key by checking for disallowed characters.
    /// </summary>
    /// <param name="tableKey">The key string to validate.</param>
    /// <returns><see langword="true"/> if the key contains no disallowed characters; otherwise, <see langword="false"/>.</returns>
    /// <seealso href="https://stackoverflow.com/questions/11514707/azure-table-storage-rowkey-restricted-character-patterns"/>
    public static bool IsKeyValid(this string tableKey)
    {
        return !DisallowedCharsInTableKeys.IsMatch(tableKey);
        //string sanitizedKey = DisallowedCharsInTableKeys.Replace(tableKey, disallowedCharReplacement);
    }

    /// <summary>
    /// Extracts a <see cref="DateTime"/> from the first 10 characters of a file name (expected format: <c>yyyy-MM-dd</c>).
    /// </summary>
    /// <param name="path">The file path whose name starts with a date in <c>yyyy-MM-dd</c> format, e.g. <c>2016-05-17-some-suffix.log.gz</c>.</param>
    /// <param name="kind">The <see cref="DateTimeKind"/> to assign to the returned <see cref="DateTime"/>. Defaults to <see cref="DateTimeKind.Utc"/>.</param>
    /// <returns>A <see cref="DateTime"/> representing the date encoded in the file name.</returns>
    /// <exception cref="ArgumentException">Thrown when the date portion of the file name cannot be parsed.</exception>
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

    /// <summary>Returns a partition key suitable for Azure Table Storage in the default format <c>yyMMdd</c>.</summary>
    /// <param name="thisDate">The date to convert into a partition key string.</param>
    /// <param name="format">The date format string used to produce the partition key. Defaults to <see cref="yyMMdd"/>.</param>
    /// <returns>A string representation of the date in the specified format, suitable for use as a partition key.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="thisDate"/> represents a year before 2000.</exception>
    public static string GetPartitionKey(this DateTime thisDate, string format = yyMMdd)
    {
        if (thisDate.Year < 2000) throw new ArgumentException("partitionKey only supports > year 2000");
        return thisDate.ToString(format);
    }

    /// <summary>
    /// Parses an Azure Table Storage partition key back into a <see cref="DateTime"/> using the specified format.
    /// </summary>
    /// <param name="thisPartitionKey">The partition key string to parse.</param>
    /// <param name="format">The date format string used when the partition key was created. Defaults to <see cref="yyMMdd"/>.</param>
    /// <returns>A <see cref="DateTime"/> (UTC) represented by the partition key.</returns>
    public static DateTime GetDateFromPartitionKey(this string thisPartitionKey, string format = yyMMdd)
        => DateTime.ParseExact(thisPartitionKey, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    private const long ticksInADay = 863999999999;

    /// <summary>
    /// Returns only the time-of-day portion of <paramref name="thisDate"/> as a 12-digit string row key.
    /// </summary>
    /// <param name="thisDate">The date and time value from which to derive the row key.</param>
    /// <param name="lexicalOrder">
    /// When <see langword="true"/> (the default), the tick count is inverted so that newer records sort to the top
    /// of an Azure Table Storage lexicographical query. Set to <see langword="false"/> to store ticks in ascending order.
    /// </param>
    /// <returns>A zero-padded 12-digit string representing the intra-day tick count, suitable for use as an Azure Table Storage row key.</returns>
    public static string GetRowKey(this DateTime thisDate, bool lexicalOrder = true)
    {
        var dayTicks = new DateTime(thisDate.Year, thisDate.Month, thisDate.Day).Ticks;
        var todayTicks = thisDate.Ticks - dayTicks;
        if (lexicalOrder) todayTicks = ticksInADay - todayTicks;
        var output = todayTicks.ToString("d12");
        return output;
    }

    /// <summary>
    /// Reconstructs the full <see cref="DateTime"/> from a row key and a partition key that encodes the date.
    /// </summary>
    /// <param name="thisRowKey">The row key string produced by <see cref="GetRowKey"/>.</param>
    /// <param name="PartitionKey">The partition key string produced by <see cref="GetPartitionKey"/> that encodes the date component.</param>
    /// <param name="lexicalOrder">
    /// When <see langword="true"/> (the default), the tick count is treated as inverted (lexicographic order).
    /// Must match the value used when the row key was generated.
    /// </param>
    /// <returns>A UTC <see cref="DateTime"/> reconstructed from the row key and partition key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="thisRowKey"/> or <paramref name="PartitionKey"/> is <see langword="null"/>.</exception>
    public static DateTime GetDateTimeFromRowKey(this string thisRowKey, string PartitionKey, bool lexicalOrder = true)
    {
        thisRowKey = thisRowKey ?? throw new ArgumentNullException(nameof(thisRowKey));
        PartitionKey = PartitionKey ?? throw new ArgumentNullException(nameof(PartitionKey));
        var dt = PartitionKey.GetDateFromPartitionKey();
        return GetDateTimeFromRowKey(thisRowKey, dt, lexicalOrder);
    }

    /// <summary>
    /// Reconstructs the full <see cref="DateTime"/> from a row key and a date representing the day component.
    /// </summary>
    /// <param name="thisRowKey">The row key string produced by <see cref="GetRowKey"/>.</param>
    /// <param name="dt">The <see cref="DateTime"/> representing the date (day) component, typically obtained from the partition key.</param>
    /// <param name="lexicalOrder">
    /// When <see langword="true"/> (the default), the tick count is treated as inverted (lexicographic order).
    /// Must match the value used when the row key was generated.
    /// </param>
    /// <returns>A UTC <see cref="DateTime"/> reconstructed from the row key and date.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="thisRowKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="thisRowKey"/> cannot be parsed as a numeric tick count.</exception>
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
