namespace AutoAnalyst.Library.Data;

/// <summary>
/// Configuration for a stratified random sampling batch with backups.
/// </summary>
/// <param name="MappingTableName">The database table containing stratum definitions.</param>
/// <param name="OutputTableName">The database table to output the final interleaved sample to.</param>
/// <param name="RandomSeed">The base random number generator seed.</param>
/// <param name="Schema">The column mapping schema for the mapping table.</param>
/// <param name="TempTablePrefix">The prefix for per-stratum temporary sample tables.</param>
public record StratifiedSampleConfiguration(
    string MappingTableName,
    string OutputTableName,
    int RandomSeed,
    MappingTableSchema Schema,
    string TempTablePrefix = "_stratum_sample_");
