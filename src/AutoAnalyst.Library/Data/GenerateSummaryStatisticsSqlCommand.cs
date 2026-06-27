namespace AutoAnalyst.Library.Data;

/// <summary>
/// Creates and executes a SQL command that generates summary statistics for a database table
/// and stores the results in a new table.
/// </summary>
public class GenerateSummaryStatisticsSqlCommand : SqlCommandBase
{
    private readonly string _sourceTableName;
    private readonly string _destinationTableName;

    /// <summary>
    /// Creates a SQL command to generate summary statistics for a table and output the results to a new table.
    /// </summary>
    /// <param name="sourceTableName">The database table to analyze.</param>
    /// <param name="destinationTableName">The database table to store the summary statistics.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sourceTableName"/> or <paramref name="destinationTableName"/> is blank.
    /// </exception>
    public GenerateSummaryStatisticsSqlCommand(
        string sourceTableName,
        string destinationTableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationTableName);

        _sourceTableName = sourceTableName;
        _destinationTableName = destinationTableName;
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that generate summary statistics for a table and output the results to a new table.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql() =>
        $"""
        CREATE OR REPLACE TABLE {_destinationTableName.EscapeIdentifier()} AS
        SELECT * FROM (SUMMARIZE {_sourceTableName.EscapeIdentifier()});
        """;
}
