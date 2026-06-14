using System.Collections.Frozen;

namespace AutoAnalyst.Library.Data;

public static class SqlCommandStringBuilder
{
    public static string AppendCommands(IEnumerable<string> commands)
    {
        return string.Join(";\n\n", commands);
    }

    /// <summary>
    /// Generates SQL command text to pull a random sample of rows from a specified table in the database and create 
    /// a new table with the results. The sample is generated using reservoir sampling, which allows for efficient
    /// sampling of large datasets. A sample_id column is added to the resulting table to provide a unique identifier 
    /// for each row in the sample.
    /// </summary>
    /// <param name="sourceTableName">The name of the table from which to pull the sample</param>
    /// <param name="sampleTableName">The name of the table to create with the contents of the sample.</param>
    /// <param name="sampleSize">The number of rows to return.</param>
    /// <param name="randomSeed">A random number generator seed.</param>
    /// <returns>The generated SQL command text</returns>
    /// <exception cref="ArgumentException">Thrown when sourceTableName is null, empty, or only whitespace; when
    /// sampleTableName is null, empty, or only whitespace; when sampleSize is negative; or when randomSeed is
    /// negative.</exception>
    public static string GetPullSampleCommand(
        string sourceTableName,
        string sampleTableName,
        int sampleSize,
        int randomSeed
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sampleTableName);
        if (sampleSize < 0)
        {
            throw new ArgumentException("Sample size must be a non-negative integer.", nameof(sampleSize));
        }
        if (randomSeed < 0)
        {
            throw new ArgumentException("Random seed must be a non-negative integer.", nameof(randomSeed));
        }

        var sequenceName = $"{sampleTableName}_sample_id_sequence";
        var returnValue = $"""
            SET threads = 1;
            CREATE OR REPLACE SEQUENCE {sequenceName};            
            CREATE OR REPLACE TABLE {sampleTableName.EscapeIdentifier()} AS
            SELECT
            "sample_id": nextval('{sequenceName}'),
            *,
            "random_number_generator_seed": {randomSeed}
            FROM {sourceTableName.EscapeIdentifier()}
            USING SAMPLE RESERVOIR({sampleSize} ROWS)
            REPEATABLE({randomSeed});
            RESET threads;
            """;

        return returnValue;
    }

    /// <summary>
    /// Generates SQL command text to pull a random sample of rows from a specified table in the database plus a backup
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
    /// <returns>The generated SQL command text</returns>
    /// <exception cref="ArgumentException">Thrown when sourceTableName is null, empty, or only whitespace; when
    /// backupTableName is null, empty, or only whitespace; when primarySampleSize is negative; when backupSampleSiz
    /// is negative; or when randomSeed is negative.</exception>
    public static string GetPullSampleWithBackupsCommand(
        string sourceTableName,
        string sampleTableName,
        int primarySampleSize,
        int backupSampleSize,
        int randomSeed,
        string primarySampleCategoryName = "Primary",
        string backupSampleCategoryName = "Backup"
    )
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

        var combinedSampleSize = primarySampleSize + backupSampleSize;
        var sequenceName = $"{sampleTableName}_sample_id_sequence";
        var returnValue = $"""
            SET threads = 1;
            CREATE OR REPLACE SEQUENCE {sequenceName};
            CREATE OR REPLACE TABLE {sampleTableName.EscapeIdentifier()} AS
            SELECT
                "sample_id",
                CASE
                    WHEN "sample_id" <= {primarySampleSize} THEN '{primarySampleCategoryName}'
                    ELSE '{backupSampleCategoryName}'
                END AS "sample_type",
                *,
                {randomSeed} AS "random_number_generator_seed"
            FROM (
                SELECT nextval('{sequenceName}') AS "sample_id", *
                FROM {sourceTableName.EscapeIdentifier()}
                USING SAMPLE RESERVOIR({combinedSampleSize} ROWS)
                REPEATABLE({randomSeed})
            );
            RESET threads;
            """;

        return returnValue;
    }
}
