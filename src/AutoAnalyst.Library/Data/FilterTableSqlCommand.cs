namespace AutoAnalyst.Library.Data;

/// <summary>
/// Creates and executes a SQL command that filters a table using a predicate over its existing columns
/// and stores the matching rows in a new table.
/// </summary>
public class FilterTableSqlCommand : SqlCommandBase
{
    private readonly string _sourceTableName;
    private readonly string _destinationTableName;
    private readonly string _filterCondition;

    /// <summary>
    /// Creates a SQL command to filter a table and output the matching rows to a new table.
    /// </summary>
    /// <param name="sourceTableName">The database table to filter.</param>
    /// <param name="destinationTableName">The database table to store the filtered rows in.</param>
    /// <param name="filterCondition">A SQL predicate that references columns from the source table.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when a table name or filter condition is blank or whitespace-only.
    /// </exception>
    public FilterTableSqlCommand(
        string sourceTableName,
        string destinationTableName,
        string filterCondition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(filterCondition);

        _sourceTableName = sourceTableName;
        _destinationTableName = destinationTableName;
        _filterCondition = filterCondition;
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that copies rows matching the filter condition into the destination table.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql() =>
        $"""
        CREATE OR REPLACE TABLE {_destinationTableName.EscapeIdentifier()} AS
        SELECT *
        FROM {_sourceTableName.EscapeIdentifier()}
        WHERE {_filterCondition};
        """;
}