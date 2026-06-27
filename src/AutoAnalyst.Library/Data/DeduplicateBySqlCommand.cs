namespace AutoAnalyst.Library.Data;

/// <summary>
/// Creates and executes a SQL command that deduplicates a table by keeping only the first
/// occurrence of each unique combination of the specified key fields, using the ROW_NUMBER()
/// window function, and stores the result in a new table.
/// </summary>
public class DeduplicateBySqlCommand : SqlCommandBase
{
    private readonly string _sourceTableName;
    private readonly string _deduplicatedTableName;
    private readonly string[] _keyFieldNames;
    private readonly string _orderByFieldName;

    /// <summary>
    /// Creates a SQL command to deduplicate a table, keeping only the first occurrence of each unique
    /// combination of the specified key fields and output the deduplicated table to a new table.
    /// </summary>
    /// <param name="sourceTableName">The database table to deduplicate.</param>
    /// <param name="deduplicatedTableName">The database table to output the deduplicated dataset to.</param>
    /// <param name="keyFieldNames">The set of key fields for which a unique combined value defines a unique record.</param>
    /// <param name="orderByFieldName">The field to use to order records that are in within each partition for row numbering</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sourceTableName"/> or <paramref name="deduplicatedTableName"/> is blank.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="keyFieldNames"/> is <c>null</c>.
    /// </exception>
    public DeduplicateBySqlCommand(
        string sourceTableName,
        string deduplicatedTableName,
        IEnumerable<string> keyFieldNames,
        string orderByFieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(deduplicatedTableName);
        ArgumentNullException.ThrowIfNull(keyFieldNames);
        ArgumentException.ThrowIfNullOrWhiteSpace(orderByFieldName);

        _sourceTableName = sourceTableName;
        _deduplicatedTableName = deduplicatedTableName;
        _keyFieldNames = [.. keyFieldNames];
        _orderByFieldName = orderByFieldName;

        if (_keyFieldNames.Length == 0)
        {
            throw new ArgumentException("A list of key field names must be provided.", nameof(keyFieldNames));
        }
        if (_keyFieldNames.Any(name => string.IsNullOrWhiteSpace(name)))
        {
            throw new ArgumentException("The list of key field names cannot contain blank or whitespace-only strings.", nameof(keyFieldNames));
        }
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that creates a deduplicated version of the source table by selecting distinct 
    /// records based on the specified key fields. The deduplication is performed using the ROW_NUMBER() window 
    /// function. Within each partition defined by the unique combinations of the specified key fields, records are 
    /// ordered by the specified order by field, and a row number is assigned to each record. The resulting 
    /// deduplicated table includes only the first occurrence of each unique combination of key field values (i.e.,
    /// records with a row number of 1). This approach allows for more flexible deduplication based on specific key 
    /// fields rather than all fields, while still ensuring that only one record per unique key combination is retained
    /// in the deduplicated output.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql()
    {
        var escapedKeyFieldNames = _keyFieldNames.Select(name => name.EscapeIdentifier());
        var keyFieldsList = string.Join(", ", escapedKeyFieldNames);
        return $"""
            CREATE OR REPLACE TABLE {_deduplicatedTableName.EscapeIdentifier()} AS
            SELECT DISTINCT *
            FROM {_sourceTableName.EscapeIdentifier()}
            QUALIFY ROW_NUMBER() OVER (PARTITION BY {keyFieldsList} ORDER BY {_orderByFieldName.EscapeIdentifier()}) = 1;
            """;
    }
}
