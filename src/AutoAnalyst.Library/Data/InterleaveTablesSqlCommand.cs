namespace AutoAnalyst.Library.Data;

/// <summary>
/// Identifies a source table and its corresponding stratum name for interleaving.
/// </summary>
public record SourceTableEntry(string SourceTableName, string StratumName);

/// <summary>
/// Creates a SQL command that merges multiple source tables into a single destination table
/// using UNION ALL BY NAME, adding <c>stratum_name</c> and <c>stratum_position</c> columns
/// to identify the source of each row, and ordering the result for round-robin interleaving
/// by <c>sample_id</c> and <c>stratum_position</c>.
/// </summary>
public class InterleaveTablesSqlCommand : SqlCommandBase
{
    private readonly SourceTableEntry[] _sourceTables;
    private readonly string _destinationTableName;

    /// <summary>
    /// Creates a SQL command to interleave tables and output the resulting combined table to a new table.
    /// </summary>
    /// <param name="sourceTables">
    /// The source tables and their corresponding stratum names. The position of each item in the
    /// enumeration determines its <c>stratum_position</c> value (1-based).
    /// </param>
    /// <param name="destinationTableName">The database table to output the interleaved result to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sourceTables"/> is empty, contains a blank source table name,
    /// contains a blank stratum name, or <paramref name="destinationTableName"/> is blank.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceTables"/> is <c>null</c>.</exception>
    public InterleaveTablesSqlCommand(
        IEnumerable<SourceTableEntry> sourceTables,
        string destinationTableName)
    {
        ArgumentNullException.ThrowIfNull(sourceTables);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationTableName);

        _sourceTables = [.. sourceTables];
        _destinationTableName = destinationTableName;

        if (_sourceTables.Length == 0)
        {
            throw new ArgumentException("A list of source tables must be provided.", nameof(sourceTables));
        }
        if (_sourceTables.Any(st => string.IsNullOrWhiteSpace(st.SourceTableName)))
        {
            throw new ArgumentException(
                "The list of source tables cannot contain blank or whitespace-only table names.", nameof(sourceTables));
        }
        if (_sourceTables.Any(st => string.IsNullOrWhiteSpace(st.StratumName)))
        {
            throw new ArgumentException(
                "The list of source tables cannot contain blank or whitespace-only stratum names.", nameof(sourceTables));
        }
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that combines multiple source tables into a single destination table
    /// using UNION ALL BY NAME, prepends <c>stratum_name</c> and <c>stratum_position</c> columns to
    /// each subquery, and orders the result by <c>sample_id</c> then <c>stratum_position</c> for
    /// round-robin interleaving.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql()
    {
        const string unionClause = "\nUNION ALL BY NAME\n";
        var selectStatements = _sourceTables.Select((st, i) =>
        {
            var position = i + 1;
            return
                $"SELECT " +
                $"'{st.StratumName.EscapeSingleQuote()}' AS \"stratum_name\", " +
                $"{position} AS \"stratum_position\", " +
                $"* " +
                $"FROM {st.SourceTableName.EscapeIdentifier()}";
        });
        var selectStatement = string.Join(unionClause, selectStatements);
        return $"""
            CREATE OR REPLACE TABLE {_destinationTableName.EscapeIdentifier()} AS
            {selectStatement}
            ORDER BY "sample_id", "stratum_position"
            """;
    }
}
