namespace AutoAnalyst.Library.Data;

/// <summary>
/// Defines the supported data file formats that can be imported into the database.
/// </summary>
public enum SupportedDataFileFormat
{
    Csv,
    Tsv,
    Parquet,
    Xlsx
}