namespace AutoAnalyst.Library.Data;

public class PullSampleSqlCommand : SqlCommandBase
{
    private readonly string _sourceTableName;
    private readonly string _sampleTableName;
    private readonly int _sampleSize;
    private readonly int _randomSeed;

    /// <summary>
    /// Creates a SQL command to pull a random attribute sample from a table in the database and create
    /// a new table with the results. The sample is generated using reservoir sampling, which allows for efficient
    /// sampling of large datasets. A sample_id column is added to the resulting table to provide a unique identifier
    /// for each row in the sample.
    /// </summary>
    /// <param name="sourceTableName">The database table to pull the sample from.</param>
    /// <param name="sampleTableName">The database table to output the sample to.</param>
    /// <param name="sampleSize">The number of records to output to the sample table.</param>
    /// <param name="randomSeed">A random number generator seed.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public PullSampleSqlCommand(
        string sourceTableName,
        string sampleTableName,
        int sampleSize,
        int randomSeed)
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

        _sourceTableName = sourceTableName;
        _sampleTableName = sampleTableName;
        _sampleSize = sampleSize;
        _randomSeed = randomSeed;
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that pulls a random attribute sample from the source table using reservoir
    /// sampling and creates a new table with the results. The SQL statement sets the number of threads to 1 to ensure
    /// that the sampling process is performed in a single thread, which is important for ensuring that the reservoir
    /// sampling algorithm produces a consistent sample based on the specified random seed. A sequence is created to
    /// generate unique sample_id values for each row in the sample, and an additional column is included to store the
    /// random number generator seed used for reproducibility. The resulting sample table includes the sampled records
    /// along with their corresponding sample_id and random number generator seed values.
    /// Finally, the SQL statement resets the number of threads to the default setting after the sampling process is
    /// complete.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql()
    {
        var sequenceName = $"{_sampleTableName}_sample_id_sequence";
        var sql = $"""
            SET threads = 1;
            CREATE OR REPLACE SEQUENCE {sequenceName};            
            CREATE OR REPLACE TABLE {_sampleTableName.EscapeIdentifier()} AS
            SELECT
            "sample_id": nextval('{sequenceName}'),
            *,
            "random_number_generator_seed": {_randomSeed}
            FROM {_sourceTableName.EscapeIdentifier()}
            USING SAMPLE RESERVOIR({_sampleSize} ROWS)
            REPEATABLE({_randomSeed});
            RESET threads;
            """;

        return sql;
    }
}
