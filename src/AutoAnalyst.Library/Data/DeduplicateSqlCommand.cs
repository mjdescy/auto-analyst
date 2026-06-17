namespace AutoAnalyst.Library.Data;

public class DeduplicateSqlCommand : SqlCommandBase
{
    private readonly string _sourceTableName;
    private readonly string _deduplicatedTableName;

    /// <summary>
    /// Creates a SQL command to deduplicate a table and output the deduplicated table to a new table.
    /// </summary>
    /// <param name="sourceTableName">The database table to deduplicate.</param>
    /// <param name="deduplicatedTableName">The database table to output the deduplicated dataset to.</param>
    /// <exception cref="ArgumentException"></exception>
    public DeduplicateSqlCommand(
        string sourceTableName,
        string deduplicatedTableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(deduplicatedTableName);

        _sourceTableName = sourceTableName;
        _deduplicatedTableName = deduplicatedTableName;
    }

    public override string BuildSql()
    {
        var sql = $"""
            CREATE OR REPLACE TABLE {_deduplicatedTableName.EscapeIdentifier()} AS
            SELECT DISTINCT *
            FROM {_sourceTableName.EscapeIdentifier()};
            """;

        return sql;
    }
}
