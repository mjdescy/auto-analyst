using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Data;

/// <summary>
/// Reads a mapping table from the database and produces a list of <see cref="StratumPlan"/>
/// objects describing each stratum to sample.
/// </summary>
public class StratifiedSamplePlanBuilder
{
    /// <summary>
    /// Queries the specified mapping table and returns a list of <see cref="StratumPlan"/> objects.
    /// </summary>
    /// <param name="engine">The database engine to query.</param>
    /// <param name="mappingTableName">The name of the mapping table that contains the stratum definitions.</param>
    /// <param name="stratumNameColumn">The column in the mapping table that holds the stratum name.</param>
    /// <param name="sourceTableNameColumn">The column in the mapping table that holds the source table name.</param>
    /// <param name="sampleSizeColumn">The column in the mapping table that holds the primary sample size.</param>
    /// <param name="backupSampleSizeColumn">The column in the mapping table that holds the backup sample size.</param>
    /// <returns>A list of <see cref="StratumPlan"/> objects, one per row in the mapping table.</returns>
    public List<StratumPlan> BuildFromTable(
        DatabaseEngine engine,
        string mappingTableName,
        string stratumNameColumn = "stratum_name",
        string sourceTableNameColumn = "source_table_name",
        string sampleSizeColumn = "sample_size",
        string backupSampleSizeColumn = "backup_sample_size")
    {
        var sql = $"""
            SELECT
                {stratumNameColumn.EscapeIdentifier()} AS stratum_name,
                {sourceTableNameColumn.EscapeIdentifier()} AS source_table_name,
                CAST({sampleSizeColumn.EscapeIdentifier()} AS INTEGER) AS sample_size,
                CAST({backupSampleSizeColumn.EscapeIdentifier()} AS INTEGER) AS backup_sample_size
            FROM {mappingTableName.EscapeIdentifier()}
            """;

        using var conn = new DuckDBConnection(engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();

        var strata = new List<StratumPlan>();
        while (reader.Read())
        {
            strata.Add(new StratumPlan(
                StratumName: reader.GetString(0),
                SourceTableName: reader.GetString(1),
                PrimarySampleSize: reader.GetInt32(2),
                BackupSampleSize: reader.GetInt32(3)));
        }

        return strata;
    }
}
