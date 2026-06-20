namespace AutoAnalyst.Library.Data;

/// <summary>
/// Produces a sequence of <see cref="ISqlCommand"/> objects that perform stratified random sampling
/// with backups across multiple source tables, using a mapping table for per-stratum sample sizes.
/// </summary>
public class PullStratifiedSampleWithBackupsBatch : ISqlCommandBatch
{
    private readonly string _mappingTableName;
    private readonly string _outputTableName;
    private readonly int _randomSeed;
    private readonly string _stratumNameColumn;
    private readonly string _sourceTableNameColumn;
    private readonly string _sampleSizeColumn;
    private readonly string _backupSampleSizeColumn;
    private readonly string _tempTablePrefix;

    /// <summary>
    /// Creates a batch that performs stratified random sampling with backups.
    /// </summary>
    /// <param name="mappingTableName">
    /// The database table containing the stratum definitions (stratum name, source table name,
    /// sample size, and backup sample size).
    /// </param>
    /// <param name="outputTableName">The database table to output the final interleaved sample to.</param>
    /// <param name="randomSeed">
    /// The base random number generator seed. Each stratum receives <c>randomSeed + index</c>
    /// as its seed, where index is the 0-based position of the stratum in the mapping table.
    /// </param>
    /// <param name="stratumNameColumn">The column in the mapping table that holds the stratum name.</param>
    /// <param name="sourceTableNameColumn">The column in the mapping table that holds the source table name.</param>
    /// <param name="sampleSizeColumn">The column in the mapping table that holds the primary sample size.</param>
    /// <param name="backupSampleSizeColumn">The column in the mapping table that holds the backup sample size.</param>
    /// <param name="tempTablePrefix">
    /// The prefix for per-stratum temporary sample tables. Defaults to <c>"_stratum_sample_"</c>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="mappingTableName"/> or <paramref name="outputTableName"/> is blank,
    /// or when <paramref name="randomSeed"/> is negative.
    /// </exception>
    public PullStratifiedSampleWithBackupsBatch(
        string mappingTableName,
        string outputTableName,
        int randomSeed,
        string stratumNameColumn = "stratum_name",
        string sourceTableNameColumn = "source_table_name",
        string sampleSizeColumn = "sample_size",
        string backupSampleSizeColumn = "backup_sample_size",
        string? tempTablePrefix = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mappingTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputTableName);

        if (randomSeed < 0)
        {
            throw new ArgumentException("Random seed must be a non-negative integer.", nameof(randomSeed));
        }

        _mappingTableName = mappingTableName;
        _outputTableName = outputTableName;
        _randomSeed = randomSeed;
        _stratumNameColumn = stratumNameColumn;
        _sourceTableNameColumn = sourceTableNameColumn;
        _sampleSizeColumn = sampleSizeColumn;
        _backupSampleSizeColumn = backupSampleSizeColumn;
        _tempTablePrefix = tempTablePrefix ?? "_stratum_sample_";
    }

    /// <summary>
    /// Builds the ordered sequence of SQL commands that perform stratified sampling:
    /// <list type="number">
    ///   <item>Queries the mapping table via <see cref="StratifiedSamplePlanBuilder"/>.</item>
    ///   <item>For each stratum, creates a <see cref="PullSampleWithBackupsSqlCommand"/>
    ///       with a derived random seed.</item>
    ///   <item>Creates an <see cref="InterleaveTablesSqlCommand"/> to merge all
    ///       per-stratum sample tables into the output table.</item>
    ///   <item>Creates <see cref="CustomSqlCommand"/> objects to drop the temporary
    ///       per-stratum tables.</item>
    /// </list>
    /// If the mapping table contains no rows, an empty sequence is returned.
    /// </summary>
    /// <param name="databaseEngine">The database engine used to query the mapping table.</param>
    /// <returns>The ordered sequence of SQL commands to execute.</returns>
    public IEnumerable<ISqlCommand> BuildCommands(DatabaseEngine databaseEngine)
    {
        var builder = new StratifiedSamplePlanBuilder();
        var strata = builder.BuildFromTable(
            databaseEngine,
            _mappingTableName,
            _stratumNameColumn,
            _sourceTableNameColumn,
            _sampleSizeColumn,
            _backupSampleSizeColumn);

        if (strata.Count == 0)
        {
            yield break;
        }

        var tempTableEntries = new List<(string SourceTableName, string StratumName)>();

        for (var i = 0; i < strata.Count; i++)
        {
            var stratum = strata[i];
            var tempTableName = $"{_tempTablePrefix}{stratum.StratumName}";

            tempTableEntries.Add((tempTableName, stratum.StratumName));

            yield return new PullSampleWithBackupsSqlCommand(
                sourceTableName: stratum.SourceTableName,
                sampleTableName: tempTableName,
                primarySampleSize: stratum.PrimarySampleSize,
                backupSampleSize: stratum.BackupSampleSize,
                randomSeed: _randomSeed + i);
        }

        yield return new InterleaveTablesSqlCommand(tempTableEntries, _outputTableName);

        foreach (var entry in tempTableEntries)
        {
            yield return new CustomSqlCommand($"DROP TABLE IF EXISTS {entry.SourceTableName.EscapeIdentifier()}");
        }
    }
}
