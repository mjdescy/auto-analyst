namespace AutoAnalyst.Library.Data;

/// <summary>
/// Creates and executes a SQL command that appends multiple source tables together
/// into a single destination table using UNION ALL BY NAME.
/// </summary>
public class AppendTablesSqlCommand : SqlCommandBase
{
    private readonly string[] _sourceTableNames;
    private readonly string _destinationTableName;

    /// <summary>
    /// Creates a SQL command to append tables and output the resulting combined table to a new table.
    /// </summary>
    /// <param name="sourceTableNames">The database tables to append together.</param>
    /// <param name="destinationTableName">The database table to output the resulting combined table to.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public AppendTablesSqlCommand(
        IEnumerable<string> sourceTableNames,
        string destinationTableName)
    {
        ArgumentNullException.ThrowIfNull(sourceTableNames);

        _sourceTableNames = [.. sourceTableNames];

        if (_sourceTableNames.Length == 0)
        {
            throw new ArgumentException("A list of source table names must be provided.", nameof(sourceTableNames));
        }
        if (_sourceTableNames.Any(name => string.IsNullOrWhiteSpace(name)))
        {
            throw new ArgumentException("The list of source table names cannot contain blank or whitespace-only strings.", nameof(sourceTableNames));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(destinationTableName);

        _destinationTableName = destinationTableName;
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that combines multiple source tables into a single destination table
    /// using UNION ALL BY NAME, preserving a column identifying the source table for each row.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql()
    {
        const string sourceColumnAlias = "source_table_name_for_append";
        const string unionClause = "\nUNION ALL BY NAME\n";
        var sourceTableSelectStatements = _sourceTableNames
            .Select(name => $"SELECT '{name.EscapeSingleQuote()}' AS \"{sourceColumnAlias}\", * FROM {name.EscapeIdentifier()}");
        var selectStatement = string.Join(unionClause, sourceTableSelectStatements);

        return $"""
            CREATE OR REPLACE TABLE {_destinationTableName.EscapeIdentifier()} AS
            {selectStatement}
            """;
    }
}
