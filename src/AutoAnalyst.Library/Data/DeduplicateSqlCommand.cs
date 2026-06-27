namespace AutoAnalyst.Library.Data;

/// <summary>
/// Creates and executes a SQL command that deduplicates a table based on all fields
/// and stores the deduplicated result in a new table using SELECT DISTINCT.
/// </summary>
public class DeduplicateSqlCommand : SqlCommandBase
{
    private readonly string _sourceTableName;
    private readonly string _deduplicatedTableName;

    /// <summary>
    /// Creates a SQL command to deduplicate a table based on all fields and output the deduplicated table to a new
    /// table.
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

    /// <summary>
    /// Builds a DuckDB SQL statement that creates a deduplicated version of the source table by selecting distinct
    /// records based on all fields. The deduplication is performed using the SELECT DISTINCT statement, which retains
    /// only unique records in the resulting deduplicated table. This approach is a straightforward way to remove
    /// duplicate records from the source table, but it considers all fields when determining uniqueness, meaning that
    /// only records that are identical across all fields will be considered duplicates and removed in the deduplicated
    /// output.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql() =>
        $"""
        CREATE OR REPLACE TABLE {_deduplicatedTableName.EscapeIdentifier()} AS
        SELECT DISTINCT *
        FROM {_sourceTableName.EscapeIdentifier()};
        """;
}
