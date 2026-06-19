namespace AutoAnalyst.Library.Data;

public class PullSampleWithBackupsSqlCommand : SqlCommandBase
{
    private readonly string _sourceTableName;
    private readonly string _sampleTableName;
    private readonly int _primarySampleSize;
    private readonly int _backupSampleSize;
    private readonly int _randomSeed;
    private readonly string _primarySampleCategoryName;
    private readonly string _backupSampleCategoryName;

    /// <summary>
    /// Creates a SQL command to pull a random sample of rows from a specified table in the database plus a backup
    /// sample from the same source table (a continuation of the sample) and create a new table with the results. The
    /// sample is generated using reservoir sampling, which allows for efficient sampling of large datasets.
    /// A sample_id column is added to the resulting table to provide a unique identifier for each row in the sample.
    /// </summary>
    /// <param name="sourceTableName">The name of the table from which to pull the sample</param>
    /// <param name="sampleTableName">The name of the table to create with the contents of the sample.</param>
    /// <param name="primarySampleSize">The number of sample items to return.</param>
    /// <param name="backupSampleSize">The number of backup sample items to return in addition to the primary sample;
    /// these backup sample items are a continuation of the primary sample from the source table, so if the main sample
    /// returns the first N rows from the source table, the backup rows will be the next M rows from the source table.
    /// </param>
    /// <param name="randomSeed">A random number generator seed.</param>
    /// <param name="primarySampleCategoryName">The value to output to the "sample_type" column for primary samples.</param>
    /// <param name="backupSampleCategoryName">The value to output to the "sample_type" column for backup samples.</param>
    /// <exception cref="ArgumentException"></exception>
    public PullSampleWithBackupsSqlCommand(
        string sourceTableName,
        string sampleTableName,
        int primarySampleSize,
        int backupSampleSize,
        int randomSeed,
        string primarySampleCategoryName = "Primary",
        string backupSampleCategoryName = "Backup")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sampleTableName);
        if (primarySampleSize < 0)
        {
            throw new ArgumentException("Primary sample size must be a non-negative integer.", nameof(primarySampleSize));
        }
        if (backupSampleSize < 0)
        {
            throw new ArgumentException("Backup sample size must be a non-negative integer.", nameof(backupSampleSize));
        }
        if (randomSeed < 0)
        {
            throw new ArgumentException("Random seed must be a non-negative integer.", nameof(randomSeed));
        }

        _sourceTableName = sourceTableName;
        _sampleTableName = sampleTableName;
        _primarySampleSize = primarySampleSize;
        _backupSampleSize = backupSampleSize;
        _randomSeed = randomSeed;
        _primarySampleCategoryName = primarySampleCategoryName;
        _backupSampleCategoryName = backupSampleCategoryName;
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
        var combinedSampleSize = _primarySampleSize + _backupSampleSize;
        var sequenceName = $"{_sampleTableName}_sample_id_sequence";
        var sql = $"""
            SET threads = 1;
            CREATE OR REPLACE SEQUENCE {sequenceName};
            CREATE OR REPLACE TABLE {_sampleTableName.EscapeIdentifier()} AS
            SELECT
                "sample_id",
                CASE
                    WHEN "sample_id" <= {_primarySampleSize} THEN '{_primarySampleCategoryName}'
                    ELSE '{_backupSampleCategoryName}'
                END AS "sample_type",
                *,
                {_randomSeed} AS "random_number_generator_seed"
            FROM (
                SELECT nextval('{sequenceName}') AS "sample_id", *
                FROM {_sourceTableName.EscapeIdentifier()}
                USING SAMPLE RESERVOIR({combinedSampleSize} ROWS)
                REPEATABLE({_randomSeed})
            );
            RESET threads;
            """;

        return sql;
    }
}
