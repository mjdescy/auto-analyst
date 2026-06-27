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
    /// <param name="schema">The column mapping schema for the mapping table.</param>
    /// <returns>A list of <see cref="StratumPlan"/> objects, one per row in the mapping table.</returns>
    public List<StratumPlan> BuildFromTable(
        DatabaseEngine engine,
        string mappingTableName,
        MappingTableSchema? schema = null)
    {
        var s = schema ?? MappingTableSchema.Default;
        var sql = $"""
            SELECT
                {s.StratumNameColumn.EscapeIdentifier()} AS stratum_name,
                {s.SourceTableNameColumn.EscapeIdentifier()} AS source_table_name,
                CAST({s.SampleSizeColumn.EscapeIdentifier()} AS INTEGER) AS sample_size,
                CAST({s.BackupSampleSizeColumn.EscapeIdentifier()} AS INTEGER) AS backup_sample_size
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
