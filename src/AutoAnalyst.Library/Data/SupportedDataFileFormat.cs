namespace AutoAnalyst.Library.Data;

/// <summary>
/// Defines the supported data file formats that can be imported to or exported from the database.
/// </summary>
public enum SupportedDataFileFormat
{
    /// <summary>Comma-separated values format.</summary>
    Csv,

    /// <summary>Tab-separated values format.</summary>
    Tsv,

    /// <summary>Microsoft Excel Open XML format.</summary>
    Xlsx,

    /// <summary>Apache Parquet columnar storage format.</summary>
    Parquet,

    /// <summary>JSON (newline-delimited) format.</summary>
    Json
}
