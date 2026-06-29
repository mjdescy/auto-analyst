namespace AutoAnalyst.Library.Data;

/// <summary>
/// Creates and executes a SQL command that analyzes text fields in a database table,
/// computing the row count for each unique value in the field, and stores the results in a new table.
/// </summary>
public class AnalyzeTextFieldsSqlCommand : SqlCommandBase
{
    private readonly string _sourceTableName;
    private readonly string[] _textFieldNames;
    private readonly string _destinationTableName;

    /// <summary>
    /// Creates a SQL command to analyze text fields in a table and output the results to a new table.
    /// </summary>
    /// <param name="sourceTableName">The database table that contains the text fields to analyze.</param>
    /// <param name="textFieldNames">The name of each text field to analyze.</param>
    /// <param name="destinationTableName">The database table to output the resulting combined table to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sourceTableName"/> is blank,
    /// <paramref name="textFieldNames"/> is empty or contains a blank or whitespace-only name,
    /// or <paramref name="destinationTableName"/> is blank.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="textFieldNames"/> is <c>null</c>.
    /// </exception>
    public AnalyzeTextFieldsSqlCommand(
        string sourceTableName,
        IEnumerable<string> textFieldNames,
        string destinationTableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTableName);
        ArgumentNullException.ThrowIfNull(textFieldNames);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationTableName);

        _sourceTableName = sourceTableName;
        _textFieldNames = [.. textFieldNames];
        _destinationTableName = destinationTableName;

        if (_textFieldNames.Length == 0)
        {
            throw new ArgumentException("A list of text field names must be provided.", nameof(textFieldNames));
        }
        if (_textFieldNames.Any(name => string.IsNullOrWhiteSpace(name)))
        {
            throw new ArgumentException("The list of text field names cannot contain blank or whitespace-only strings.", nameof(textFieldNames));
        }
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that produces an analysis of the specified text fields and outputs the results
    /// to a new destination table.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql() =>
        $"""
        CREATE OR REPLACE TABLE {_destinationTableName.EscapeIdentifier()} AS
        WITH unpivoted AS (
            UNPIVOT {_sourceTableName.EscapeIdentifier()}
            ON {string.Join(", ", _textFieldNames.Select(name => name.EscapeIdentifier()))}
            INTO
                NAME column_name
                VALUE raw_value
        ),
        normalized AS (
            SELECT
                column_name,
                value: COALESCE(raw_value::VARCHAR, '<NULL>')
            FROM unpivoted
        )
        SELECT
            column_name,
            value,
            record_count: COUNT(*)
        FROM normalized
        GROUP BY column_name, value
        ORDER BY column_name, record_count DESC;
        """;
}
