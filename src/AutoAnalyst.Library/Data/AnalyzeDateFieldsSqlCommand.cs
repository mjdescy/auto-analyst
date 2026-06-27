namespace AutoAnalyst.Library.Data;

/// <summary>
/// Creates and executes a SQL command that analyzes date fields in a database table,
/// computing statistics such as min and max dates, unique days present, missing day counts,
/// and validity counts, and stores the results in a new table.
/// </summary>
public class AnalyzeDateFieldsSqlCommand : SqlCommandBase
{
    private readonly string _sourceTableName;
    private readonly string[] _dateFieldNames;
    private readonly string _destinationTableName;

    /// <summary>
    /// Creates a SQL command to analyze date fields in a table and output the results to a new table.
    /// </summary>
    /// <param name="sourceTableName">The database table that contains the date fields to analyze.</param>
    /// <param name="dateFieldNames">The name of each date field to analyze.</param>
    /// <param name="destinationTableName">The database table to output the resulting combined table to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sourceTableName"/> is blank,
    /// <paramref name="dateFieldNames"/> is empty or contains a blank or whitespace-only name,
    /// or <paramref name="destinationTableName"/> is blank.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dateFieldNames"/> is <c>null</c>.</exception>
    public AnalyzeDateFieldsSqlCommand(
        string sourceTableName,
        IEnumerable<string> dateFieldNames,
        string destinationTableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTableName);

        _sourceTableName = sourceTableName;

        ArgumentNullException.ThrowIfNull(dateFieldNames);

        _dateFieldNames = [.. dateFieldNames];

        if (_dateFieldNames.Length == 0)
        {
            throw new ArgumentException("A list of date field names must be provided.", nameof(dateFieldNames));
        }
        if (_dateFieldNames.Any(name => string.IsNullOrWhiteSpace(name)))
        {
            throw new ArgumentException("The list of date field names cannot contain blank or whitespace-only strings.", nameof(dateFieldNames));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(destinationTableName);

        _destinationTableName = destinationTableName;
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that produces an analysis of the specified date fields and outputs the results to
    /// a new destination table.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql()
    {
        var dataTableNamesSqlClause = string.Join(", ", _dateFieldNames.Select(name => name.EscapeIdentifier()));
        return $"""
            CREATE OR REPLACE TABLE {_destinationTableName.EscapeIdentifier()} AS
            WITH unpivoted AS (
                UNPIVOT {_sourceTableName.EscapeIdentifier()}
                ON {dataTableNamesSqlClause}
                INTO
                    NAME column_name
                    VALUE raw_value
            ),
            with_validity AS (
                SELECT
                    column_name,
                    raw_value,
                    value_status: CASE
                        WHEN raw_value = '' THEN 'null'
                        WHEN raw_value IS NULL THEN 'null'
                        WHEN try_cast(raw_value::VARCHAR AS DATE) IS NULL THEN 'invalid'
                        ELSE 'valid'
                    END,
                    date_value: try_cast(raw_value::VARCHAR AS DATE)
                FROM unpivoted
            )
            SELECT
                column_name,
                min_date: MIN(date_value)::DATE,
                max_date: MAX(date_value)::DATE,
                unique_days_present: COUNT(DISTINCT date_value::DATE),
                missing_days_count: COALESCE((MAX(date_value)::DATE - MIN(date_value)::DATE + 1)
                    - COUNT(DISTINCT date_value::DATE),
                    0
                ),
                total_rows: COUNT(*),
                null_count: COUNT(*) FILTER (WHERE value_status = 'null'),
                invalid_count: COUNT(*) FILTER (WHERE value_status = 'invalid'),
                valid_count: COUNT(*) FILTER (WHERE value_status = 'valid')
            FROM with_validity
            GROUP BY column_name
            ORDER BY column_name;
            """;
    }
}
