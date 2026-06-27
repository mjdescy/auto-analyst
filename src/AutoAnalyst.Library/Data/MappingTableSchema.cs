namespace AutoAnalyst.Library.Data;

/// <summary>
/// Describes the column names in a mapping table that defines per-stratum sampling parameters
/// for stratified sampling workflows.
/// </summary>
/// <param name="StratumNameColumn">The column holding the stratum name.</param>
/// <param name="SourceTableNameColumn">The column holding the source table name.</param>
/// <param name="SampleSizeColumn">The column holding the primary sample size.</param>
/// <param name="BackupSampleSizeColumn">The column holding the backup sample size.</param>
public record MappingTableSchema(
    string StratumNameColumn,
    string SourceTableNameColumn,
    string SampleSizeColumn,
    string BackupSampleSizeColumn)
{
    /// <summary>
    /// A default schema using conventional column names.
    /// </summary>
    public static readonly MappingTableSchema Default = new(
        StratumNameColumn: "stratum_name",
        SourceTableNameColumn: "source_table_name",
        SampleSizeColumn: "sample_size",
        BackupSampleSizeColumn: "backup_sample_size");
}
