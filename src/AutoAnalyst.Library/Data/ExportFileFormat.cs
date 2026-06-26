namespace AutoAnalyst.Library.Data;

/// <summary>
/// Specifies the file format to use when exporting a table.
/// </summary>
public enum ExportFileFormat
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
