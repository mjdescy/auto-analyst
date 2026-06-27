namespace AutoAnalyst.Library.Data;
/// <summary>
/// Produces a sequence of <see cref="ISqlCommand"/> objects that perform stratified random sampling
/// with backups across multiple source tables, using a mapping table for per-stratum sample sizes.
/// </summary>
public class PullStratifiedSampleWithBackupsBatch : ISqlCommandBatch
{
    private readonly StratifiedSampleConfiguration _config;

    /// <summary>
    /// Creates a batch that performs stratified random sampling with backups.
    /// </summary>
    /// <param name="config">The configuration for the stratified sampling operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="config"/> contains a blank mapping table name or output table name,
    /// or a negative random seed.
    /// </exception>
    public PullStratifiedSampleWithBackupsBatch(StratifiedSampleConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.MappingTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.OutputTableName);

        if (config.RandomSeed < 0)
        {
            throw new ArgumentException("Random seed must be a non-negative integer.", nameof(config));
        }

        _config = config;
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
            _config.MappingTableName,
            _config.Schema);

        if (strata.Count == 0)
        {
            yield break;
        }

        var tempTableEntries = new List<SourceTableEntry>();

        for (var i = 0; i < strata.Count; i++)
        {
            var stratum = strata[i];
            var tempTableName = $"{_config.TempTablePrefix}{stratum.StratumName}";

            tempTableEntries.Add(new SourceTableEntry(tempTableName, stratum.StratumName));

            yield return new PullSampleWithBackupsSqlCommand(
                new SampleConfiguration(
                    SourceTableName: stratum.SourceTableName,
                    SampleTableName: tempTableName,
                    PrimarySampleSize: stratum.PrimarySampleSize,
                    BackupSampleSize: stratum.BackupSampleSize,
                    RandomSeed: _config.RandomSeed + i));
        }

        yield return new InterleaveTablesSqlCommand(tempTableEntries, _config.OutputTableName);

        foreach (var entry in tempTableEntries)
        {
            yield return new CustomSqlCommand($"DROP TABLE IF EXISTS {entry.SourceTableName.EscapeIdentifier()}");
        }
    }
}
