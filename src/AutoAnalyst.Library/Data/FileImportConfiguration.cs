namespace AutoAnalyst.Library.Data;

/// <summary>
/// Describes the parameters for importing a file (or files matching a glob pattern)
/// into a database table.
/// </summary>
/// <param name="Format">The format of the data files to import.</param>
/// <param name="GlobPattern">A glob pattern defining which file or files to import.</param>
/// <param name="TableName">The database table to import the data into.</param>
/// <param name="TypeHints">Optional column type hints for delimited formats; ignored for Parquet.</param>
public record FileImportConfiguration(
    SupportedDataFileFormat Format,
    string GlobPattern,
    string TableName,
    ColumnTypeHints? TypeHints = null);
