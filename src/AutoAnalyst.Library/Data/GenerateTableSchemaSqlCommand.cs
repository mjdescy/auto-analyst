namespace AutoAnalyst.Library.Data;

/// <summary>
/// Creates and executes a SQL command that generates a table schema for a table and stores it in a new table.
/// </summary>
public class GenerateTableSchemaSqlCommand : SqlCommandBase
{
    private readonly string _sourceTableName;
    private readonly string _destinationTableName;

    /// <summary>
    /// Creates a SQL command to generate a table schema for a table and output the results to a new table.
    /// </summary>
    /// <param name="sourceTableName">The database table for which to generate the schema.</param>
    /// <param name="destinationTableName">The database table to store the generated schema.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sourceTableName"/> or <paramref name="destinationTableName"/> is blank.
    /// </exception>
    public GenerateTableSchemaSqlCommand(
        string sourceTableName,
        string destinationTableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationTableName);

        _sourceTableName = sourceTableName;
        _destinationTableName = destinationTableName;
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that generates the schema for a table and output the results to a new table.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql() =>
        $"""
        CREATE OR REPLACE TABLE {_destinationTableName.EscapeIdentifier()} AS
        SELECT
            ordinal_position,
            column_name, 
            data_type, 
            is_nullable, 
            column_default, 
            character_maximum_length, 
            numeric_precision, 
            numeric_scale
        FROM information_schema.columns
        WHERE table_schema IN (
                SELECT table_schema
                FROM duckdb_tables()
                WHERE table_name = '{_sourceTableName.EscapeSingleQuote()}'
              )
          AND table_name = '{_sourceTableName.EscapeSingleQuote()}'
        ORDER BY ordinal_position;
        """;
}
