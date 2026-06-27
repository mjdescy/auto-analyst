namespace AutoAnalyst.Library.Data;

/// <summary>
/// Creates and executes a SQL command that analyzes numeric fields in a database table,
/// computing statistics such as min, max, mean, median, standard deviation, skewness,
/// kurtosis, quartiles, and validity counts, and stores the results in a new table.
/// </summary>
public class AnalyzeNumericFieldsSqlCommand : SqlCommandBase
{
    private const double LowerQuartile = 0.25;
    private const double UpperQuartile = 0.75;

    private readonly string _sourceTableName;
    private readonly string[] _numericFieldNames;
    private readonly string _destinationTableName;

    /// <summary>
    /// Creates a SQL command to analyze numeric fields in a table and output the results to a new table.
    /// </summary>
    /// <param name="sourceTableName">The database table that contains the numeric fields to analyze.</param>
    /// <param name="numericFieldNames">The name of each numeric field to analyze.</param>
    /// <param name="destinationTableName">The database table to output the resulting combined table to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sourceTableName"/> is blank,
    /// <paramref name="numericFieldNames"/> is empty or contains a blank or whitespace-only name,
    /// or <paramref name="destinationTableName"/> is blank.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="numericFieldNames"/> is <c>null</c>.
    /// </exception>
    public AnalyzeNumericFieldsSqlCommand(
        string sourceTableName,
        IEnumerable<string> numericFieldNames,
        string destinationTableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTableName);
        ArgumentNullException.ThrowIfNull(numericFieldNames);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationTableName);

        _sourceTableName = sourceTableName;
        _numericFieldNames = [.. numericFieldNames];
        _destinationTableName = destinationTableName;

        if (_numericFieldNames.Length == 0)
        {
            throw new ArgumentException("A list of numeric field names must be provided.", nameof(numericFieldNames));
        }
        if (_numericFieldNames.Any(name => string.IsNullOrWhiteSpace(name)))
        {
            throw new ArgumentException("The list of numeric field names cannot contain blank or whitespace-only strings.", nameof(numericFieldNames));
        }
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that produces an analysis of the specified numeric fields and outputs the results
    /// to a new destination table.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql() =>
        $"""
        CREATE OR REPLACE TABLE {_destinationTableName.EscapeIdentifier()} AS
        WITH unpivoted AS (
            UNPIVOT {_sourceTableName.EscapeIdentifier()}
            ON {string.Join(", ", _numericFieldNames.Select(name => name.EscapeIdentifier()))}
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
                    WHEN try_cast(raw_value::VARCHAR AS DOUBLE) IS NULL THEN 'invalid'
                    ELSE 'valid'
                END,
                numeric_value: try_cast(raw_value::VARCHAR AS DOUBLE)
            FROM unpivoted
        )
        SELECT
            column_name,
            min_value: MIN(numeric_value),
            max_value: MAX(numeric_value),
            total_sum: SUM(numeric_value),
            mean: AVG(numeric_value),
            median_value: MEDIAN(numeric_value),
            std_dev: STDDEV_SAMP(numeric_value),
            skewness: SKEWNESS(numeric_value),
            kurtosis: KURTOSIS(numeric_value),
            q1: PERCENTILE_CONT({LowerQuartile}) WITHIN GROUP (ORDER BY numeric_value),
            q3: PERCENTILE_CONT({UpperQuartile}) WITHIN GROUP (ORDER BY numeric_value),
            unique_values_count: COUNT(DISTINCT numeric_value),
            total_rows: COUNT(*),
            null_count: COUNT(*) FILTER (WHERE value_status = 'null'),
            invalid_count: COUNT(*) FILTER (WHERE value_status = 'invalid'),
            valid_count: COUNT(*) FILTER (WHERE value_status = 'valid'),
            zero_count: COUNT(*) FILTER (WHERE numeric_value = 0),
            negative_count: COUNT(*) FILTER (WHERE numeric_value < 0),
            positive_count: COUNT(*) FILTER (WHERE numeric_value > 0)
        FROM with_validity
        GROUP BY column_name
        ORDER BY column_name;
        """;
}
