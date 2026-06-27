namespace AutoAnalyst.Library.Data;

/// <summary>
/// Describes the parameters for pulling a random sample with backups from a database table.
/// </summary>
/// <param name="SourceTableName">The database table to pull the sample from.</param>
/// <param name="SampleTableName">The database table to output the sample to.</param>
/// <param name="PrimarySampleSize">The number of rows to include in the primary sample.</param>
/// <param name="BackupSampleSize">The number of backup rows to pull in addition to the primary sample.</param>
/// <param name="RandomSeed">A random number generator seed.</param>
/// <param name="PrimaryCategory">The value for the <c>sample_type</c> column for primary samples.</param>
/// <param name="BackupCategory">The value for the <c>sample_type</c> column for backup samples.</param>
public record SampleConfiguration(
    string SourceTableName,
    string SampleTableName,
    int PrimarySampleSize,
    int BackupSampleSize,
    int RandomSeed,
    string PrimaryCategory = "Primary",
    string BackupCategory = "Backup");
