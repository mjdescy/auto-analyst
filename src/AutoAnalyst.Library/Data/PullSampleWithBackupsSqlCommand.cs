namespace AutoAnalyst.Library.Data;

/// <summary>
/// Creates and executes a SQL command that pulls a random sample of rows from a database table
/// plus a backup continuation sample using reservoir sampling, categorizing each row as
/// primary or backup, and stores the results in a new table.
/// </summary>
public class PullSampleWithBackupsSqlCommand : SqlCommandBase
{
    private readonly SampleConfiguration _request;

    /// <summary>
    /// Creates a SQL command to pull a random sample of rows from a specified table in the database plus a backup
    /// sample from the same source table (a continuation of the sample) and create a new table with the results. The
    /// sample is generated using reservoir sampling, which allows for efficient sampling of large datasets.
    /// A sample_id column is added to the resulting table to provide a unique identifier for each row in the sample.
    /// </summary>
    /// <param name="request">The parameters describing the sample to pull.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="request"/> contains a blank source or sample table name,
    /// a negative sample size, or a negative random seed.
    /// </exception>
    public PullSampleWithBackupsSqlCommand(SampleConfiguration request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SampleTableName);
        if (request.PrimarySampleSize < 0)
        {
            throw new ArgumentException("Primary sample size must be a non-negative integer.", nameof(request));
        }
        if (request.BackupSampleSize < 0)
        {
            throw new ArgumentException("Backup sample size must be a non-negative integer.", nameof(request));
        }
        if (request.RandomSeed < 0)
        {
            throw new ArgumentException("Random seed must be a non-negative integer.", nameof(request));
        }

        _request = request;
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that pulls a random sample of rows from a specified table in the database plus a
    /// backup sample from the same source table (a continuation of the sample) and creates a new table with the 
    /// results. The SQL statement sets the number of threads to 1 to ensure that the sampling process is performed in a
    /// single thread, which is important for ensuring that the reservoir sampling algorithm produces a consistent
    /// sample based on the specified random seed. A sequence is created to generate unique sample_id values for each
    /// row in the sample, and an additional column is included to store the random number generator seed used for
    /// reproducibility. The resulting sample table includes the sampled records along with their corresponding
    /// sample_id and random number generator seed values, as well as a "sample_type" column that indicates whether
    /// each row is part of the primary sample or the backup sample based on the specified category names.
    /// Finally, the SQL statement resets the number of threads to the default setting after the sampling process is
    /// complete. 
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql()
    {
        var combinedSampleSize = _request.PrimarySampleSize + _request.BackupSampleSize;
        var sequenceName = $"{_request.SampleTableName}_sample_id_sequence";
        return $"""
            SET threads = 1;
            CREATE OR REPLACE SEQUENCE {sequenceName};
            CREATE OR REPLACE TABLE {_request.SampleTableName.EscapeIdentifier()} AS
            SELECT
                "sample_id",
                CASE
                    WHEN "sample_id" <= {_request.PrimarySampleSize} THEN '{_request.PrimaryCategory}'
                    ELSE '{_request.BackupCategory}'
                END AS "sample_type",
                *,
                {_request.RandomSeed} AS "random_number_generator_seed"
            FROM (
                SELECT nextval('{sequenceName}') AS "sample_id", *
                FROM {_request.SourceTableName.EscapeIdentifier()}
                USING SAMPLE RESERVOIR({combinedSampleSize} ROWS)
                REPEATABLE({_request.RandomSeed})
            );
            RESET threads;
            """;
    }
}
