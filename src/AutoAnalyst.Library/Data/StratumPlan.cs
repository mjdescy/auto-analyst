namespace AutoAnalyst.Library.Data;

/// <summary>
/// Describes the sampling parameters for a single stratum within a stratified sampling workflow.
/// </summary>
/// <param name="StratumName">The human-readable name of the stratum (e.g., "EastRegion").</param>
/// <param name="SourceTableName">The database table from which to pull the sample.</param>
/// <param name="PrimarySampleSize">The number of rows to include in the primary sample.</param>
/// <param name="BackupSampleSize">The number of backup sample rows to pull in addition to the primary sample.</param>
public record StratumPlan(
    string StratumName,
    string SourceTableName,
    int PrimarySampleSize,
    int BackupSampleSize);
